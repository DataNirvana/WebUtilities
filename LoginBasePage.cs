using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MGL.Web.WebUtilities;
using MGL.Data.DataUtilities;
using System.IO;
using System.Text;
using MGL.Security;


//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Security {

    //----------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     21-Apr-2016 - The LoginBasePage used for setting up the Login, Logout, PasswordRequestReset and PasswordReset pages
    ///     
    ///     Note that this page inherits the PagePersistViewStateToFileSystem base page AND the MGL base page
    ///     Storing the view state on the server for these security pages is a recommended security feature.
    ///     
    ///     Note that this base page is within the MGL.Security namespace - it should be all accounts be in the MGL.Security project, but as that is a feeder to the 
    ///     WebUtilities project (which contains the other two base pages), we have to put it together in the WebUtilities project
    /// </summary>
    public class LoginBasePage : PagePersistViewStateToFileSystem {


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     21-Apr-2016 - if the user has stopped cookies from running, then we need a default that is also 36 characters long ...
        ///     Possible exploitation here in that a malicious user could turn off cookies on purpose, but they still need to log in ultimately, so all they get is not killing the previous session
        /// </summary>
        private static string DefaultAnonID = "123456789012345678901234567890123456";


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     The page to redirect to if the security checks were valid.  If the checks failed the user will be redirected to the default post login page
        ///     Action can be one of Login, PasswordRequestReset or PasswordReset
        ///     ASSUMPTION is that the action page HAS TO BE IN THE Code/Security/ folder!!
        /// </summary>
        protected string ActionPage = "Login";


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Called the first time the login page is requested.  For security, on new logins, the current session should be
        ///     abandoned and a new session started.
        ///
        ///     Calls the login page back with the ResetSession and an encrypted CID token which contains the AnonID
        ///     cookie value (a GUID) and a date time stamp
        ///
        /// </summary>
        protected void Step1KillSession() {

            string redirectURL = MGLApplicationSecurityInterface.Instance().AppLoginConfig.DefaultPostLoginPage;

            try {

                //-----1----- Abandon the current session
                // 20-Apr-2016 - Before killing the session lets make absolutely sure that the current Session ID has been removed from the HTTPS checks
                // This should be entirely unnecessary and does appear to be so, but lets do it anyway as it is light, fast and important
                //Logger.Log("XXXXX - "+ActionPage+".aspx - removing the session requires https for " + Session.SessionID + " BEFORE Killing the session....");
                if (MGLApplicationInterface.Instance().UseHttpsSessionsIndependently == true) {
                    MGLApplicationInterface.Instance().RemoveSessionRequiringHTTPS(Session.SessionID);
                    MGLSessionInterface.Instance().UseHTTPS = false;
                }
                // And then lets abandon the session properly
                Session.Abandon();
                Response.Cookies.Add(new HttpCookie("ASP.NET_SessionId", ""));

                //-----2----- Build the encrypted string
                StringBuilder authKey = GenerateKey();

                //-----3----- Build the redirectURL, including the special action, next page URL and CID authorisation token
                // For the independent HTTPS in sessions we need to call this page again to configure the new session and then continue
                // otherwise we can just go with it ...
                string redirectPage = (MGLApplicationInterface.Instance().UseHttpsSessionsIndependently == false) ? ActionPage + "Do.aspx" : ActionPage + ".aspx";
                redirectURL = BuildRedirectURL(redirectPage, authKey);

            } catch (Exception ex) {
                Logger.LogError(8, "Problem setting the authorisation redirect url in the login page.  This is serious!  The specific error was: " + ex.ToString());
            }

            //-----4----- Redirect to the loginDo page and commence the login event in anger.  
            // Or if this a use-https-in-sessions-independently website, lets come back to the login page to reconfigure.
            Response.Redirect(redirectURL);

        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Called the second time the login page is requested.  For security, on new logins, the current session should be
        ///     abandoned and a new session started.  
        ///     
        ///     If HTTPS is globally all false or globally all true, this intermediate step is not required as the page requesting the login page will 
        ///     have the same protocol (both HTTP or both HTTPS).  However, if the MGLApplicationInterface.UseHttpsSessionsIndependently variable is set, then we need
        ///     an intermediate step - this step - to force the session to use HTTPs and then call LoginDo...
        ///
        ///     Calls the LoginDo page with the ResetSession and an encrypted CID token which contains the AnonID
        ///     cookie value ( a GUID) and a date time stamp.
        ///
        /// </summary>
        protected void Step2SetSecureSession(StringBuilder encryptedKey) {

            string redirectURL = MGLApplicationSecurityInterface.Instance().AppLoginConfig.DefaultPostLoginPage;

            try {

                bool keyIsValid = KeyIsValid(encryptedKey);
                //Logger.Log("XXXXX - "+ActionPage+".aspx - Setting the secure session.  And checking that the key is valid: " + keyIsValid);

                //-----a----- Get the AuthKey and check it is legit - if not we do nothing and the user is bounced out to the default page
                if (keyIsValid == true) {

                    //-----b----- Setup the new session to be secure - which by now should have been created as this page loads for the second time!
                    // Force this session to use HTTPs as this page has been requested
                    if (MGLSessionInterface.Instance().UseHTTPS == false) {
                        MGLSessionInterface.Instance().SetSessionRequiresHTTPs(Request.Cookies["AnonID"], Session.SessionID, HttpContext.Current.Request.IsLocal);
                    }

                    //-----c----- Build the URL for the LoginDo page - and if HTTPS is enabled, we explicitly set the LoginDo page to use HTTPS
                    // ASSUMPTION is that the action page HAS TO BE IN THE Code/Security folder!!
                    string redirectPage = ActionPage + "Do.aspx";
                    if (MGLSessionInterface.Instance().UseHTTPS == true && HttpContext.Current.Request.IsLocal == false) {
                        redirectPage = "https://" + MGLSessionInterface.Instance().Config.WebProjectPath() + "Code/Security/" + redirectPage;
                    }

                    redirectURL = BuildRedirectURL(redirectPage, encryptedKey);
                }

            } catch (Exception ex) {
                Logger.LogError(8, "Problem setting the secure session in the login page.  This is serious!  The specific error was: " + ex.ToString());
            }
            //Logger.Log("XXXXX - " + ActionPage + ".aspx - https is " + MGLSessionInterface.Instance().UseHTTPS + " and Redirecting to : " + redirectURL);
            
            //-----d----- Redirect to the LoginDo page and commence the login event in anger ...
            Response.Redirect(redirectURL);
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Checks whether or not the Key in the CID parameter for this page is valid (this computer based on the AnonID) and the time stamp is current
        /// </summary>
        protected string BuildRedirectURL(string pageName, StringBuilder authKey) {

            StringBuilder redirectURL = new StringBuilder();

            //-----1----- Append our page name and the reset session action
            redirectURL.Append(pageName);
            redirectURL.Append("?Action=ResetSession");

            //-----2a----- For LOGINS - Get the next page to go to once a login event has successfully been completed ...
            string url = Request.Params.Get("NextPage");
            if (string.IsNullOrEmpty(url) == false) {
                redirectURL.Append("&NextPage=" + url);
            }

            //-----2b----- For FAILED PasswordResets - which are then referred back the PasswordRequestReset ....
            string resetReferralError = Request.Params.Get("ResetReferralError");
            if (string.IsNullOrEmpty(resetReferralError) == false) {
                redirectURL.Append("&ResetReferralError=" + resetReferralError);
            }

            //-----2c----- For real PasswordResets - lets include the token, which when decrypted is the key to the PasswordReset.GetWidget(...)
            string token = Request.Params.Get("Token");
            if (string.IsNullOrEmpty(token) == false) {
                redirectURL.Append("&Token=" + token);
            }
            
            //-----3----- And our encrypted credentials
            redirectURL.Append("&CID=" + authKey);

            return redirectURL.ToString();
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Modifies the given next page URL to explicitly declare using HTTPS, if this is required at the application or session level
        /// </summary>
        protected string BuildNextPageURL(string url, bool httpsRequired) {

            // 20-Apr-2016 - modify the next page URL so that it automatically goes to the HTTPs page if required
            if (httpsRequired == true) {
                if (url.StartsWith("http://") == true) {
                    url = url.Replace("http://", "https://");
                } else if (url.StartsWith("~/") == true) {
                    url = url.Replace("~/", "https://" + MGLSessionInterface.Instance().Config.WebProjectPath());
                }
            } else {
                if (url.StartsWith("https://") == true) {
                    url = url.Replace("https://", "http://");
                } else if (url.StartsWith("~/") == true) {
                    url = url.Replace("~/", "http://" + MGLSessionInterface.Instance().Config.WebProjectPath());
                }
            }

            return url;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Generates the key with the temporal and computer specific attributes
        /// </summary>
        protected StringBuilder GenerateKey() {

            //-----a----- Start building the encrypted string
            StringBuilder tokenToEncrypt = new StringBuilder();
            tokenToEncrypt.Append(MGLEncryption.GetSalt(1));

            //-----b----- Double check that the anonID cookie has been set and set it again if not... (this should never not have happened as it is called first OnInit in the MGLBasePage)
            SetAnonIDCookie();

            //-----c----- The AnonID is a GUID - always 36 characters
            string tempValue = DefaultAnonID;
            if (Request.Cookies["AnonID"] != null) {
                tempValue = Request.Cookies["AnonID"].Value;
            }
            tokenToEncrypt.Append(tempValue);

            //-----d----- Add some padding
            tokenToEncrypt.Append(MGLEncryption.GetSalt(1));

            //-----e----- The date time will be of the form yyyy-mm-dd hh:mm:ss - and will always be dd and mm (not d and m for e.g. 01)
            tokenToEncrypt.Append(DateTimeInformation.FormatDatabaseDate(DateTime.Now, true, true));
            tokenToEncrypt.Append(MGLEncryption.GetSalt(1));

            //-----f----- Do the encryption itself
            StringBuilder authKey = MGLEncryption.Encrypt(tokenToEncrypt);

            //-----g----- And lastly, lets make this key HTML secure ...
            authKey = MGLEncryption.HTMLifyString(authKey);

            return authKey;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Checks whether or not the Key in the CID parameter for this page is valid (this computer based on the AnonID) and the time stamp is current
        ///     If the key should be extracted from the Page URL parameters, just set the encryptedKey to null... 
        /// </summary>
        protected bool KeyIsValid(StringBuilder encryptedKey) {

            bool keyIsValid = false;

            try {

                // 11-May-2016 - the MGLEncryption.Decrypt method throws a serious level 9 error if no key is provided.  
                // It is better to catch this here and simply return false as this is just due to people not using the tool correctly (or trying to cut corners)
                if (encryptedKey != null && encryptedKey.Length > 0) {

                    //-----a----- Decrypt the key ...
                    encryptedKey = MGLEncryption.DeHTMLifyString(encryptedKey);
                    StringBuilder decryptedKey = MGLEncryption.Decrypt(encryptedKey);

                    //-----b----- Pull out the anon ID
                    StringBuilder anonID = new StringBuilder(decryptedKey.ToString().Substring(1, 36));
                    StringBuilder dtStr = new StringBuilder(decryptedKey.ToString().Substring(38, 19));

                    //-----c----- now check that the dt is within tolerances
                    DateTime dt;
                    DateTime.TryParse(dtStr.ToString(), out dt);

                    TimeSpan ts = DateTime.Now.Subtract(dt);

                    //-----d----- get the anonvalue cookie again ...
                    string tempValue = DefaultAnonID;
                    if (Request.Cookies["AnonID"] != null) {
                        tempValue = Request.Cookies["AnonID"].Value;
                    }

                    //-----e----- So then finally, do the validation on two fronts
                    //      a. that the elapsed time span is more than 0 and less than 10 seconds and
                    //      b. that the anonID is correct
                    keyIsValid = (ts.TotalSeconds >= 0 && ts.TotalSeconds < 10) && MGLEncryption.AreEqual(anonID, new StringBuilder(tempValue));
                }

            } catch (Exception ex) {
                Logger.LogError(8, "Problem checking if the authorisation key in the login page is valid.  This is serious!  The specific error was: " + ex.ToString());
            }

            return keyIsValid;
        }

    }
}
