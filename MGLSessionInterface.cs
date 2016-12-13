using System;
using System.Web;
using System.Collections;
using System.Reflection;
using MGL.Data.DataUtilities;
using System.Collections.Generic;

//using MGL.LLPG;
//using MGL.Security;

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Web.WebUtilities {


    //--------------------------------------------------------------------------------------------------------------------------------------------------------------
	/// <summary>
	///
	/// SessionInterface is a singlton session variable that is used to
	/// as the single point of access for all session variables.
	///
	/// </summary>
	public sealed class MGLSessionInterface {
		//Name that will be used as key for Session object
		private const string SESSION_SINGLETON = "SESSION_SINGLETON";

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
		//Private constructor so cannot create an instance
		// without using the correct method.  This is
		// this is critical to properly implementing
		// as a singleton object, objects of this
		// class cannot be created from outside this
		// class
		private MGLSessionInterface() {
			//Intialise the vars
			ClearAll();
		}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// Resets all session variables to their intial values
		/// </summary>C:\Dropbox\Dropbox (UNHCR Pakistan)\Data Nirvana\CodeProjects\MGL.Web.WebUtilities\MGLSessionInterface.cs
		public void ClearAll() {
			config = null;
//            displayedItemsList = new DisplayedItems();
            mapIsLoaded = false;
		}



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
		//Create as a static method so this can be called using
		// just the class name (no object instance is required).
		// It simplifies other code because it will always return
		// the single instance of this class, either newly created
		// or from the session
		public static MGLSessionInterface Instance() {
			MGLSessionInterface seshSingleton;

			if (null == System.Web.HttpContext.Current.Session[SESSION_SINGLETON]) {
				//No current session object exists, use private constructor to
				// create an instance, place it into the session
				seshSingleton = new MGLSessionInterface();
				System.Web.HttpContext.Current.Session[SESSION_SINGLETON] = seshSingleton;
			} else {
				//Retrieve the already instance that was already created
				seshSingleton = (MGLSessionInterface)System.Web.HttpContext.Current.Session[SESSION_SINGLETON];
			}

			return seshSingleton;

		}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Holds the currently loaded LoadConfiguration file which is used to create a DB_Operations class
        /// </summary>
        private ConfigurationInfo config;
        public ConfigurationInfo Config {
            get { return config; }
            set { config = value; }
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Session expired ....
        /// </summary>
        private bool sessionExpired = false;
        public bool SessionExpired {
            get { return sessionExpired; }
            set { sessionExpired = value; }
        }


        ////-----------------------------------------------------------------------------------------------------------------------------------------------------------
        ///// <summary>
        /////     Is First Load ....
        ///// </summary>
        //private bool isFirstLoad = false;
        //public bool IsFirstLoad {
        //    get { return isFirstLoad; }
        //    set { isFirstLoad = value; }
        //}



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private List<MGL.DomainModel.DataListInfo> listOfDataLists = null;


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        public MGL.DomainModel.DataListInfo GetDataInfo(string key) {
            if (listOfDataLists != null) {
                foreach (MGL.DomainModel.DataListInfo dl in listOfDataLists) {
                    if (dl.Equals(key)) {
                        return dl;
                    }
                }
            }
            return null;
        }


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void SetDataInfo(MGL.DomainModel.DataListInfo newList) {
            if (newList != null) {
                if (listOfDataLists == null) {
                    listOfDataLists = new List<MGL.DomainModel.DataListInfo>();
                }

                int index = -1;
                int count = 0;
                foreach (MGL.DomainModel.DataListInfo dl in listOfDataLists) {
                    if (dl.Equals(newList.Key)) {
                        index = count;
                        break;
                    }
                    count++;
                }

                if (index == -1) {    // Add it
                    listOfDataLists.Add(newList);
                } else {                 // Overwrite it
                    listOfDataLists[index] = newList;
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void RemoveDataInfo(string key) {
            if (listOfDataLists != null) {
                for (int i = 0; i < listOfDataLists.Count; i++ ) {
                    MGL.DomainModel.DataListInfo dl = listOfDataLists[i];
                    if ( dl != null && dl.Equals(key)) {
                        listOfDataLists.RemoveAt(i);
                        i--;
                    }
                }
            }
        }



        ////-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //public void ResetDataList() {
        //    dataList = null;
        //    DataListIsSorted = false;
        //    DataListIsSortedByID = false;
        //    DataListIsSortedAsc = true;
        //    DataListTotalNumberItemsAttempted = -1;
        //    DataListNumberResultsPerPage = 200;
        //    DataListCurrentPage = 1;
        //    DataListNumberOfPages = 0;
        //    DataGridSrcImgPath = "";
        //    addressList = null;
        //    DataGridDetailsURL = null;
        //}

        ////-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //private List<MGL.GEDI.DomainModel.MGLSimpleContent> dataList;
        //public List<MGL.GEDI.DomainModel.MGLSimpleContent> DataList {
        //    get { return dataList; }
        //    set { dataList = value; }
        //}

        ////-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //public bool DataListIsSorted = false;
        //public bool DataListIsSortedByID = false;
        //public bool DataListIsSortedAsc = true;
        //public int DataListTotalNumberItemsAttempted;
        //public int DataListNumberResultsPerPage;
        //public int DataListCurrentPage;
        //public int DataListNumberOfPages;
        //public string DataGridSrcImgPath;
        //public string DataGridDetailsURL;


        ////-----------------------------------------------------------------------------------------------------------------------------------------------------------
        //private List<MGL.GEDI.DomainModel.MGLAddressList> addressList;

        //public List<MGL.GEDI.DomainModel.MGLAddressList> AddressList {
        //    get { return addressList; }
        //    set { addressList = value; }
        //}

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// Hold an array of displayItems which each describe a facility or statistic currently selected for display.
        /// </summary>
        ///
        //private DisplayedItems displayedItemsList;
        //public DisplayedItems DisplayedItemsList {
        //    get {
        //        return displayedItemsList;

        //    }
        //    set {
        //        displayedItemsList = value;
        //    }
        //}


        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private string sessionID = null;
        public string SessionID {
            get { return sessionID; }
            set { sessionID = value; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        public void GenerateNewSessionID() {

            // 17-Jul-15 - converted this to use GUIDs as these are guaranteed to also be completely unique ....
            sessionID = System.Guid.NewGuid().ToString();

            //Random r = new Random();
            //sessionID = DateTime.Now.Ticks.ToString() + r.Next(0, 9) + r.Next(0, 9) + r.Next(0, 9) + r.Next(0, 9);

        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     12-Oct-16 - Moved from HASessionInterface for portability ...
        /// </summary>
        public string AsyncProcessingSeshID {
            get { return asyncProcessingSeshID; }
            set { asyncProcessingSeshID = value; }
        }
        private string asyncProcessingSeshID = null;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private bool mapIsLoaded = false;
        public bool MapIsLoaded {
            get { return mapIsLoaded; }
            set { mapIsLoaded = value; }
        }



        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        private int aRandomCounter = 1;
        public int ARandomCounter {
            get { return ++aRandomCounter; }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private int numberOfUsers;
        public int NumberOfUsers {
            get { return numberOfUsers; }
            set {
                numberOfUsers = value;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private MGL.DomainModel.ListSortState listSortState;
        public MGL.DomainModel.ListSortState ListSortState {
            get { return listSortState; }
            set {
                listSortState = value;
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        private MGL.DomainModel.ListPageState listPageState;
        public MGL.DomainModel.ListPageState ListPageState {
            get { return listPageState; }
            set {
                listPageState = value;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Used for e.g. storing an Excel spreadsheet and passing the data between pages ...
        /// </summary>
        private byte[] fileContent;
        public byte[] FileContent {
            get { return fileContent; }
            set {
                fileContent = value;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Added 24-Dec-2015 ....
        /// </summary>
        private bool useHTTPS = false;
        public bool UseHTTPS {
            get { return useHTTPS; }
            set {
                useHTTPS = value;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Probably this is a custom cookie called AnonID ....
        /// </summary>
        public void SetSessionRequiresHTTPs(HttpCookie seshCookie, string sessionID, bool isLocal) {

            if (isLocal == false
                && seshCookie != null 
                && sessionID != null 
                && MGLApplicationInterface.Instance().SessionsRequiringHTTPs != null
                ) {

                // only check if it exists based on the seshCookie, not the session ID
                // this should catch for old sessions ...
                bool existsAlready = false;
                foreach (KeyValuePair<string, string> kvp in MGLApplicationInterface.Instance().SessionsRequiringHTTPs) {

                    if (kvp.Key.Equals(seshCookie.Value)) {
                        existsAlready = true;
                        break;
                    }
                }

                // remove it if it exists
                if (existsAlready == true) {
                    MGLApplicationInterface.Instance().RemoveSessionRequiringHTTPS(sessionID);
                }
                // now add it
                MGLApplicationInterface.Instance().SessionsRequiringHTTPs.Add(new KeyValuePair<string, string>(seshCookie.Value, sessionID));

                MGLSessionInterface.Instance().UseHTTPS = true;

                //Logger.Log("XXXXX SetSessionRequiresHTTPs: " + seshCookie.Value + "    " + sessionID);

            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Added 11-May-2016 - now we are using the static resources where we can, this brings new problems with sites requiring SSL
        ///     If the user does not have the CACert certificates installed, rather than just the initial "Are you sure you want to proceed to this insecure site" warning,
        ///     The requests to the static css and js on the other site just fail silently.  
        ///     
        ///     Therefore we need to catch this, warn the user and explain what to do to resolve this and then also reconfigure the session to use the 
        ///     local resources in the interim while the user sorts his shite out.
        /// </summary>
        private bool sslError = false;
        public bool SSLError {
            get { return sslError; }
            set {
                sslError = value;
            }
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Added 24-Dec-2015 .... to override the HTMLUtilities.Default path in specific sessions
        ///     Now read only and not currently used, so keep an eye on whether or not this is going to be redundant.
        /// </summary>
        public string DefaultPath {
            get { return ((MGLSessionInterface.Instance().UseHTTPS) ? "https://" : "http://") + MGLApplicationInterface.Instance().ConfigDefault.WebProjectPath(); }
        }



	}
}
