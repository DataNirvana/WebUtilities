using System;
using System.Web;
using System.Collections;
using System.Collections.Generic;
using MGL.Data.DataUtilities;
using System.Web.Configuration;
using MGL.DomainModel;
using System.Security;
//using MGL.Security;
//using MGL.LLPG;
//using MGL.Security;

//--------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Web.WebUtilities {

    //-----------------------------------------------------------------------------------------------------------------------------------------------------------------
	/// <summary>
	///
    /// MGLApplicationInterface is a singlton application variable that is used to
    /// as the single point of access for all application variables.
	///
	/// </summary>
	public sealed class MGLApplicationInterface {
        //Name that will be used as key for application object
		private static string APP_SINGLETON = "MGL_APP_SINGLETON";

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Create as a static method so this can be called using
        // just the class name (no object instance is required).
        // It simplifies other code because it will always return
        // the single instance of this class, either newly created
        // or from the application

        // Used when not in an http context (e.g. in a test context)
        // to cache the ApplicationInterface
        private static MGLApplicationInterface cachedApp;

        public static MGLApplicationInterface Instance() {
            MGLApplicationInterface appSingleton;

            //This allows us to switch which application object
            // we are using for secure/non-secure sessions
            string APPLICATION_CACHE = APP_SINGLETON;

            // If not in an http context (e.g. in a test context)
            // use a static variable to cache the ApplicationInterface
            if (null == System.Web.HttpContext.Current)
            {
                if (cachedApp == null)
                {
                    cachedApp = new MGLApplicationInterface();
                }

                appSingleton = cachedApp;

                return appSingleton;
            }
            else if (null == System.Web.HttpContext.Current.Application[APPLICATION_CACHE]) {
                //No current Application object exists, use private constructor to
                // create an instance, place it into the Application
                appSingleton = new MGLApplicationInterface();
                System.Web.HttpContext.Current.Application[APPLICATION_CACHE] = appSingleton;
            } else {
                //Retrieve the already instance that was already created
                appSingleton = (MGLApplicationInterface)System.Web.HttpContext.Current.Application[APPLICATION_CACHE];
            }

            return appSingleton;

        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
		//Private constructor so cannot create an instance
		// without using the correct method.  This is
		// this is critical to properly implementing
		// as a singleton object, objects of this
		// class cannot be created from outside this
		// class
        private MGLApplicationInterface() {
			ClearAll();
		}

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Resets all application variables to their intial values
		/// </summary>
		public void ClearAll() {
            configDefault = null;
		}



        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private ConfigurationInfo configDefault;

        public ConfigurationInfo ConfigDefault {
            get { return configDefault; }
            set { configDefault = value; }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private string physicalPathToApplicationRoot = null;
        public string PhysicalPathToApplicationRoot {
            get { return physicalPathToApplicationRoot; }
            set { physicalPathToApplicationRoot = value; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        public readonly string UserCountFileName = "App_Data/UserCount.txt";

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private int numberOfUsers;

        public int NumberOfUsers {
            get { return numberOfUsers; }
            set {
                numberOfUsers = value;

                //_____ 2-Feb-2015 - this looks like it was just being used for testing - now removed!
                //if (numberOfUsers % 10 == 0) {
                //    SimpleIO sio = new SimpleIO();
                //    bool success = sio.WriteToFile( physicalPathToApplicationRoot + UserCountFileName,
                //        numberOfUsers.ToString(), false );

                //    string boyacasha = "";
                //}
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string NumberOfUsersString {

            // 2-Feb-2015 - awful code to get a rubbish string!  Changed to use the N0 formula!
            get {
                //string numUserStr = "";
                //if (numberOfUsers < 10) {
                //    numUserStr = "000000";
                //} else if (numberOfUsers < 100) {
                //    numUserStr = "00000";
                //} else if (numberOfUsers < 1000) {
                //    numUserStr = "0000";
                //} else if (numberOfUsers < 10000) {
                //    numUserStr = "000";
                //} else if (numberOfUsers < 100000) {
                //    numUserStr = "00";
                //} else if (numberOfUsers < 1000000) {
                //    numUserStr = "0";
                //} else if (numberOfUsers < 10000000) {
                //    numUserStr = "";
                //}
                //return numUserStr + numberOfUsers.ToString(); }
                return numberOfUsers.ToString("N0");
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string ApplicationName {
            get { return applicationName; }
            set { applicationName = value; }
        }
        private string applicationName = "Derby LLPG"; // A very cool/painful reminder of the history (ney lineage) of some of this code ...!  The name is updated in the global ASAX

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string ApplicationShortName {
            get { return applicationShortName; }
            set { applicationShortName = value; }
        }
        private string applicationShortName = ""; // e.g. PNA used for the handles on the XML files and areas where the presentation space is tighter.

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string ApplicationURL {
            get { return applicationURL; }
            set { applicationURL = value; }
        }
        private string applicationURL = "";

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public string CodeVersion {
            get { return codeVersion; }
            set { codeVersion = value; }
        }
        private string codeVersion = "0.0";

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     31-Mar-2016 - we are now using this more widely, including to stop the caching of the static resources
        ///     http://stackoverflow.com/questions/32414/how-can-i-force-clients-to-refresh-javascript-files#32427
        /// </summary>
        public string JSVersion {
            get { return jsVersion; }
            set { jsVersion = value; }
        }
        private string jsVersion = "0.0";


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
            private bool _PreventPrefixedLikeQueries = false;
        /// <summary>
        /// If true the address search queries will NOT use the %prefix
        /// in like queries. Using this will hang mySQL for
        /// massive address tables.
        /// </summary>
        public bool PreventPrefixedLikeQueries
        {
            get
            {
                if (WebConfigurationManager.AppSettings["PreventPrefixedLikeQueries"] != null)
                {
                    bool.TryParse(WebConfigurationManager.AppSettings["PreventPrefixedLikeQueries"], out _PreventPrefixedLikeQueries);
                }

                return _PreventPrefixedLikeQueries;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private int _DaysBetweenForcedPasswordChanges = 0;
        /// <summary>
        /// If greater than zero the users will be bumped to the change password
        /// (edit user settings page everytime
        /// </summary>
        public int DaysBetweenForcedPasswordChanges
        {
            get
            {
                if (  WebConfigurationManager.AppSettings["DaysBetweenForcedPasswordChanges"] != null)
                {
                    int.TryParse(WebConfigurationManager.AppSettings["DaysBetweenForcedPasswordChanges"], out _DaysBetweenForcedPasswordChanges);
                }

                return _DaysBetweenForcedPasswordChanges;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool _UseOpenSpaceMapping = false;
        public bool UseOpenSpaceMapping
        {
            get
            {
                if (WebConfigurationManager.AppSettings["UseOpenSpaceMapping"] != null)
                {
                    bool.TryParse(WebConfigurationManager.AppSettings["UseOpenSpaceMapping"], out _UseOpenSpaceMapping);
                }

                return _UseOpenSpaceMapping;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private string _ConfigFilePath = "";
        public string ConfigFilePath
        {
            get
            {
                if (_ConfigFilePath == "" && WebConfigurationManager.AppSettings["ConfigFilePath"] != null)
                {
                    _ConfigFilePath = WebConfigurationManager.AppSettings["ConfigFilePath"];
                }

                return _ConfigFilePath;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Monitors the total number of page requests the application is receiving in an hour ....
        /// </summary>
        private ApplicationMonitor appMonitor = new ApplicationMonitor();
        public ApplicationMonitor AppMonitor {
            get { return appMonitor; }
            set { appMonitor = value; }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        // add processor load and error variables ....
        private string lastError;
        public string LastError {
            get { return lastError; }
            set { lastError = value; }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Temporary encryption key for information created on the fly in the application ...
        /// </summary>
        private SecureString encryptionAppKey = null;
        public SecureString EncryptionAppKey {
            get { return encryptionAppKey; }
            set { encryptionAppKey = value; }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private ConfigurationInfo geoLocationConfig = null;
        /// <summary>
        ///     The configuration file for the geo location database
        /// </summary>
        public ConfigurationInfo GeoLocationConfig {
            get { return geoLocationConfig; }
            set { geoLocationConfig = value; }
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool useHttpsSessionsIndependently = false;
        public bool UseHttpsSessionsIndependently {
            get { return useHttpsSessionsIndependently; }
            set { useHttpsSessionsIndependently = value; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Stores the asp.net session id which is extracted from cookies for the specific sessions requiring HTTPS
        ///     Use HTTPS in a specific session for public facing sites with an admin component like the Explore.RAHAPakistan.org website ...
        ///     This list takes two values, the persistent value of AnonID and the more transient sessionID,  when a session ends, the session
        ///     ID is the ONLY way to remove this pc from the list ....
        /// </summary>
        private List<KeyValuePair<string, string>> sessionsRequiringHTTPs = new List<KeyValuePair<string,string>>();
        public List<KeyValuePair<string, string>> SessionsRequiringHTTPs {
            get { return sessionsRequiringHTTPs; }
            set { sessionsRequiringHTTPs = value; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Probably this is a custom cookie called AnonID ....
        /// </summary>
//        public bool CheckSessionRequiresHTTPs(HttpCookie seshCookie, string srcURL, bool isLocal) {
        public bool CheckSessionRequiresHTTPs(HttpCookie seshCookie, bool isLocal) {
            //HttpCookie seshCookie = Request.Cookies["ASP.NET_SessionId"];

            bool isHttps = false;
            if (UseHttpsSessionsIndependently == true) {

                if ( isLocal == false
                    && seshCookie != null && SessionsRequiringHTTPs != null ) {

                    //Logger.Log("XXXXX CheckSessionRequiresHTTPs: " + seshCookie.Value + "    " + sessionsRequiringHTTPs.Count);

                    // we are only checking each request on the machine ... the session end will kill off any persistent stuff
                    foreach( KeyValuePair<string, string> kvp in sessionsRequiringHTTPs ) {
                        //Logger.Log("XXXXX KVP: " + kvp.Key + "    " + kvp.Value);
                        if ( kvp.Key.Equals( seshCookie.Value )) {
                            isHttps = true;
                            break;
                        }
                    }
                }
            }

            return isHttps;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     And remove the session - Anon cookie ID pairing from the application list given the specific session ID
        /// </summary>
        public void RemoveSessionRequiringHTTPS(string sessionID) {

            if (sessionID != null && SessionsRequiringHTTPs != null) {
                //Logger.Log("XXXXX RemoveSessionRequiringHTTPS: " + sessionID + "   " + MGLApplicationInterface.Instance().SessionsRequiringHTTPs.Count);
                int index = -1;
                int count = 0;
                foreach (KeyValuePair<string, string> kvp in MGLApplicationInterface.Instance().SessionsRequiringHTTPs) {

                    //Logger.Log("XXXXX KVP: " + kvp.Key + "    " + kvp.Value);
                    
                    if (kvp.Value.Equals(sessionID)) {
                        index = count;
                        break;
                    }
                    count++;
                }

                if (index >= 0) {
                    sessionsRequiringHTTPs.RemoveAt(index);
                }
            }


        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - Looking to optimise the code as much as possible.  One way is to load all the static resources from a separate domain
        ///     But for local testing, we just want to use the local path, so we need to make this configurable
        /// </summary>
        public string StaticResourcePath {
            get { return staticResourcePath; }
            set { staticResourcePath = value; }
        }
        private string staticResourcePath = "~/";
        //private string staticPath = "https://static.datanirvana.org/";

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - Determines whether or not the minified version of the javascript classes should be used.  
        ///     Normally this would be false while testing and true in production.
        /// </summary>
        public bool StaticJavascriptIsMinified {
            get { return staticJavascriptIsMinified; }
            set { staticJavascriptIsMinified = value; }
        }
        private bool staticJavascriptIsMinified = false;


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     29-Mar-2016 - Turns on or off the compression achieved by removing all leading and trailing whitespace from pages 
        ///     as they are being rendered - used in the MGLBasePage - Render method ...
        ///     Why do this you ask?  This saves about 25% of the download size
        /// </summary>
        public bool RemoveWhitespaceFromAllPages {
            get { return removeWhitespaceFromAllPages; }
            set { removeWhitespaceFromAllPages = value; }
        }
        private bool removeWhitespaceFromAllPages = false;

	}
}
