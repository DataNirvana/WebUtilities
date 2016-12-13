using System;
using System.Web.UI;
using System.Configuration;
using System.Web;
using MGL.Data.DataUtilities;
using MGL.DomainModel;
using MGL.Security;
using System.Text.RegularExpressions;
using System.Text;
using DataNirvana.Database;


//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Web.WebUtilities {

    //----------------------------------------------------------------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for basePageSessionExpire.
	/// </summary>
	public class MGLBasePage : System.Web.UI.Page {

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     For Performance Benchmarking ...
        ///     26-Feb-2016 - originally the dt1 was set with the start date and time here.  This worked fine for normal requests.  However
        ///     for instances where the same page is requested multiple times from a single source page (e.g. GetPortraitImage from CaseView),
        ///     the dt1 was being reused, resulting in much longer average times that could be expected from the activities within e.g. GetPortraitImage
        ///     So, the fix is to set dt1 officially at the start of the OnInit method - easy eh.  Luckily for us, this will actually affected
        ///     very few scenarios in the current web apps, as most requests are singular.  Getting the images on the case view (with multiple individuals)
        ///     is the only scenario I can actually think of that will be affected by this.  The result is in the logging, the average page response times
        ///     will look far too high!
        /// </summary>
        //DateTime dt1 = DateTime.Now;
        DateTime dt1 = DateTimeInformation.NullDate;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string functionality = null;

        public string Functionality {
            get { return functionality; }
            set { functionality = value; }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool isSecurePage = true;

        public bool IsSecurePage {
            get { return isSecurePage; }
            set { isSecurePage = value; }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        private MGPageType mgPageType = MGPageType.ASPX;

        public MGPageType MgPageType {
            get { return mgPageType; }
            set { mgPageType = value; }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool requireRedirectOnSessionEnd = true;

        public bool RequireRedirectOnSessionEnd {
            get { return requireRedirectOnSessionEnd; }
            set { requireRedirectOnSessionEnd = value; }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Determines whether or not the current request is using a secure connection ...
        /// </summary>
        public static bool CurrentRequestIsUsingSecureConnection() {
            return HttpContext.Current.Request.IsSecureConnection;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Give the URL prefix based on whether or not the current request is using a secure connection ...
        /// </summary>
        public static string URLPrefix( bool secure ) {
            if ( secure == true ) {
                return "https://";
            } else {
                return "http://";
            }
        }




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
		public MGLBasePage() {
            // empty login .....
		}




        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        protected override void OnInit(EventArgs e) {
            // 26-Feb-2016 Set the performance time start during the start of the onInit
            dt1 = DateTime.Now;

            base.OnInit(e);

              // 24-Dec-2015 (Merry Xmas!!!)
            // check for a special cookie and set it if it is not there already ..  This is for a persistent computer ID to test the session against ...
            SetAnonIDCookie();
            //HttpCookie ck = Request.Cookies["AnonID"];
            //if (ck == null || ck.Value == null) {
            //    Response.Cookies.Add(new HttpCookie("AnonID", System.Guid.NewGuid().ToString())); // Session.SessionID));//
            //}

            if (SessionTimedOut()) {

                // if its a secure page, check the security
                if (isSecurePage) {

                    // check for credentials ...
                    //if (CheckCredentials() == false) {

                        // Redirect to the login page if this is not an AJAX request ... after the login page, the default redirect is to the default page ...
                        if (IsAJAXRequest(false) == false) {

                            // 28-Jan-2015 - added the referral for next page to the login in this context.  This means that the user wont get ripped back to the default page unnecessarily.  There will
                            // be some pages that do not work in this kind of context (e.g. results of a search stored in a session) and therefore these individual pages will have to now respond
                            // as they should ...

                            // 20-Apr-2016 - lets ensure that the call to the login page use https, if not local

                            string nextPage = GetNextPage();
                            Response.Redirect(MGL.Security.Authorisation.LoginPage + "?NextPage=" + nextPage);
                        }
                    //}
                } else {
                    // March 2012 - modified so that the redirect only happens if the redirect is specified ...
                    // Go to the root page ... But ONLY if it is required
                    // Oct 2013 - Added the IsAJAXRequest check ...
                    if (IsAJAXRequest(false) == false && requireRedirectOnSessionEnd) {
                        Response.Redirect(MGL.Security.Authorisation.DefaultPage);
                    } else {
                        // Do Nothing - the SessionExpired flag will already have been set by SessionTimed out method, if this is not the default page and AJAX call backs will also be triggered ...
                    }
                }

            } else {

                // if its a secure page, check the security
                if (isSecurePage) {

                    // check the user is logged in
                    if (MGL.Security.Authorisation.DoIsLoggedIn() == false) {

                        // If not logged in, check the credentials using the emailHash
                        //if (CheckCredentials() == false) {

                        // 20-Apr-2016 - lets ensure that the call to the login page use https, if not local

                            string nextPage = GetNextPage();
                            Response.Redirect(MGL.Security.Authorisation.LoginPage + "?NextPage=" + nextPage);
                        //}

                    }
                    // check the user has the permissions to view this page
                    if (functionality != null && functionality != "") {
                        if (MGL.Security.Authorisation.DoIsAuthorised(MGL.Security.Authorisation.Functionality, functionality) == false) {
                            Response.Redirect(MGL.Security.Authorisation.NoEntryPage);
                        }
                    }
                } else {
                    // Do Nothing!  All is good!
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Strims the leading white space from all lines
        ///     WARNING - Note that this has not yet been load tested....
        ///     The concept is from http://optimizeasp.net/remove-whitespace-from-html
        ///     This saves about 25% of the download size
        /// </summary>
        protected override void Render(HtmlTextWriter writer) {
            using (HtmlTextWriter htmlwriter = new HtmlTextWriter(new System.IO.StringWriter())) {
                base.Render(htmlwriter);

                // Only remove the leading and trailing whitespace from pages, if this has been explicitly set in the application level variables
                if (MGLApplicationInterface.Instance().RemoveWhitespaceFromAllPages == false) {
                    writer.Write(htmlwriter.InnerWriter);
                } else {
                    //string html = htmlwriter.InnerWriter.ToString();
                    // Trim the leading and trailing whitespace from the 'html' variable
                    ////html = Regex.Replace(html, "[ \t]*[\r]?\n[ \t]*", "\r\n"); 
                    //html = R.Replace(html, "\r\n");
                    //writer.Write(html);

                    // lets try concatenating all the objects to reduce the memory load slightly ...
                    writer.Write(R.Replace(htmlwriter.InnerWriter.ToString(), "\r\n"));
                }
            }
        }
        // The regex as a static variable to optimise this strimming as much as possible ...  See this good resource for more regexes http://regexhero.net/reference/
        private static Regex R = new Regex("[ \t]*[\r]?\n[ \t]*");

        //---------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Using the OnUnload method for performance benchmarking of pages
        /// </summary>
        protected override void OnUnload(EventArgs e) {

            // Performance benchmarking ...
            DateTime dt2 = DateTime.Now;
            TimeSpan t1 = dt2.Subtract(dt1);

            string currentPageName = HttpContext.Current.Request.Url.AbsoluteUri;

            int currentUserID = (Authorisation.CurrentUser != null) ? Authorisation.CurrentUser.ID : 0;

            // 14-Jan-2015 - Get the IP Address
            string srcIPAddress = IPAddressHelper.GetIP4OrAnyAddressFromHTTPRequest();

            // 11-Mar-2016 - Sanity check - when pages crash, dt1 is not always set so is the null date, so if the difference between dt1 and dt2 is more than one day (!!), use dt2
            // 16-Mar-2016 - And reset the timespan so it is sensible, otherwise the average response time queries in view page performance do not work!
            // you can find the crashed pages in the db with this query: SELECT * FROM Log_PageRequests WHERE Server_Render_Speed > 63593600000000;
            DateTime logTime = dt1;
            if (t1.TotalMilliseconds > 86400000) {
                logTime = dt2;
                t1 = new TimeSpan(0, 0, 0, 0, 50);
            }

            LoggerDB.LogPageRequestInDatabase(MGLSessionInterface.Instance().Config, MGLApplicationInterface.Instance().ApplicationName,
                Session.SessionID, MGLSessionInterface.Instance().SessionID, currentPageName, dt2, t1.TotalMilliseconds, currentUserID, srcIPAddress);
//            Logger.LogError(currentPageName + "Time to build page: " + t1.TotalMilliseconds);

            base.OnUnload(e);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>Checks whether or not the session has timed out</summary>
        /// <returns></returns>
        protected bool SessionTimedOut() {
            bool sessionHasTimedOut = false;

            //It appears from testing that the Request and Response both share the
            // same cookie collection.  If I set a cookie myself in the Reponse, it is
            // also immediately visible to the Request collection.  This just means that
            // since the ASP.Net_SessionID is set in the Session HTTPModule (which
            // has already run), thatwe can't use our own code to see if the cookie was
            // actually sent by the agent with the request using the collection. Check if
            // the given page supports session or not (this tested as reliable indicator
            // if EnableSessionState is true), should not care about a page that does
            // not need session
            if (Context.Session != null) {
                //Tested and the IsNewSession is more advanced then simply checking if
                // a cookie is present, it does take into account a session timeout, because
                // I tested a timeout and it did show as a new session
                if (Session.IsNewSession) {
                    // If it says it is a new session, but an existing cookie exists, then it must
                    // have timed out (can't use the cookie collection because even on first
                    // request it already contains the cookie (request and response
                    // seem to share the collection)
                    string szCookieHeader = Request.Headers["Cookie"];
                    if ((null != szCookieHeader) && (szCookieHeader.IndexOf("ASP.NET_SessionId") >= 0)) {

                        sessionHasTimedOut = true;

                        // Finally - if the user is going straight back to the default page anyway, do we care that the Session has timed out?
                        // No it is irrelevant.  Also, and more importantly - this means that we also ignore the case where a user comes to the site, does something
                        // then goes a way and comes back - after a day or so, it would be surprising to have the session expired notice come up
                        // 28-Jan-2015 - All this does is pop up a message on the default page to say that the session has expired!
                        // It does not change any of the other functionality with the logins / security pages ...
                        if (CurrentPageIsDefaultPage() == false) {
                            MGLSessionInterface.Instance().SessionExpired = true;
                        }
                    }
                }
            }
            return sessionHasTimedOut;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>Checks whether or not the session has timed out and returns the Javascript that will cause the whole containing page to be reloaded</summary>
        /// <returns></returns>
        protected bool IsAJAXRequest( bool checkSessionExpired) {
            bool isAJax = false;

            if ((mgPageType == MGPageType.AJAX_HTML || mgPageType == MGPageType.AJAX_JavaScript)
                && (checkSessionExpired == false || (checkSessionExpired == true && SessionTimedOut()))) {

                if (mgPageType == MGPageType.AJAX_HTML) {
                    Response.Write("<script language='javascript' type='text/javascript'>");
//                    Response.Write("alert('In MGLBasePage');");
                } else {
//                    Response.Write("alert('In MGLBasePage - just doing the javascript ..... ');");
                }

                Response.Write("DoAJAXSessionExpired();");

                if (mgPageType == MGPageType.AJAX_HTML) {
                    Response.Write("</script>");
                }

                isAJax = true;
            }

            return isAJax;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Checks to see if the Key, a hash of the users email has been passed to this page.  If so, an attempt is made to extract
        ///     the Users credentials using a combination of this key, the users IP address and a Timespan, probably about 8 hours since
        ///     the initial login.  Credentials are only valid if all three aspects are legitimate.
        ///
        ///     12-Oct-2015 - this is not secure and all this code should be stripped out!!
        /// </summary>
        /// <returns></returns>
        //protected bool CheckCredentials() {
        //    bool credentialsOk = false;

        //    // check to see if there is a key - this will be passed if using the external checks on security ...
        //    string emailHash = Request.Params.Get("Key");
        //    if (emailHash != null && emailHash != "") {
        //        credentialsOk = MGL.Security.Authorisation.ApplyUserCredentials(emailHash);
        //    }
        //    return credentialsOk;
        //}


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Returns true if the current page is the default page ...
        /// </summary>
        protected bool CurrentPageIsDefaultPage() {
            bool success = false;

            // get the page name from the current page ...
            string currentPageURL = Request.Url.AbsoluteUri;
            string[] cpBits = currentPageURL.Split(new string[] { "/" }, StringSplitOptions.None);
            string[] cpBits2 = cpBits[cpBits.Length - 1].Split(new string[] { "?" }, StringSplitOptions.None);

            // do the same from the default page ...
            string[] dBits = MGL.Security.Authorisation.DefaultPage.Split(new string[] { "/" }, StringSplitOptions.None);
            string[] dBits2 = dBits[dBits.Length - 1].Split(new string[] { "?" }, StringSplitOptions.None);

            if (cpBits2[0].Equals(dBits2[0], StringComparison.CurrentCultureIgnoreCase)) {
                success = true;
            }

            return success;

        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        protected string GetNextPage() {

            //Steve edit 2/6/09. The addition of the ?Next page parameter seems to screw up the "~" character
            // i.e when the login page is defined as "~/Code/Security/Login.aspx" the redirect to root only
            // happens when the "NextPage= + nextPage" is not there!
            // 30-Jan-10 - ES changed redirect back as seems to work ok when using the nextPage!
            // Adding the http prefix to the next page seemed to cause the issues.  This now works in EST / Tower Hamlets / DNF.
            // Which other systems use this base page?  Addressing IT / Service Directory / GEDI
            string nextPage = Request.Path;

            // 20-Oct-16 - Killing all of this as it doesnt seem to work in VS2015!
            //if (nextPage.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) == false) {
            //    // TODO: Switch on http or https ....
            //    nextPage = "http://" + MGLSessionInterface.Instance().Config.WebHost + Request.Path;
            //}

            //// 6 Feb 2010
            //// Check to see if the web root of the login page is external - if it is then add the current web root to the nextPage variable
            //// This is useful if the login page is external to the current web site
            //string loginPage = MGL.Security.Authorisation.LoginPage.ToLower();
            //string webHost = MGLSessionInterface.Instance().Config.WebHost.ToLower();
            //string webRoot = MGLSessionInterface.Instance().Config.WebRoot;
            //if (webRoot != null) {
            //    webRoot = webRoot.ToLower();
            //}
            //if (loginPage.StartsWith("~/") == false &&
            //    ((loginPage.Contains(webHost) == false) || (webRoot != null && webRoot != "" && loginPage.Contains(webRoot)) == false)) {

            //    nextPage = "http://" + webHost + nextPage;
            //}


            return nextPage;
        }


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Sets the anonID cookie, if it did not already exist ...  This is a persistent cookie that should persist beyond the lifetime of the application ..
        /// </summary>
        protected void SetAnonIDCookie() {
            // check for a special cookie and set it if it is not there already ..  This is for a persistent computer ID to test the session against ...
            HttpCookie ck = Request.Cookies["AnonID"];
            if (ck == null || ck.Value == null) {
                Response.Cookies.Add(new HttpCookie("AnonID", System.Guid.NewGuid().ToString())); // Session.SessionID));//
            }
        }

        
	}

}
