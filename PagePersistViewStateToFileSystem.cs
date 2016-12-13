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


//----------------------------------------------------------------------------------------------------------------------------------------------------------
/// <summary>
///     16-Mar-2016 - The viewstate of some of the pages is creeping ever larger as the number of elements in the forms increases
///     This is particularly the case for CaseNewStep1, IndividualNew and IndividualUpdate, but is also true of the CaseNewStep2 and CaseUpdate.
///
///     The genius whizzkids at Microsoft have come up with a useful solution - instead of appending the view state to specific pages, which is then download and uploaded
///     write it to a temporary file.  The background is explained here: https://msdn.microsoft.com/en-us/library/ms972976.aspx?f=255&MSPPError=-2147217396
///
///     As there will be a small performance hit on the server, we do not want to do this for all pages.  But, by extending the MGLBasePage with this Page wrapper
///     for specific pages, we can override the default functionality for the specific pages where this is more of an issue.
///
///     For reference, a quick poll of "heavy" but useful pages online finds that the FT front page is 150kb; Hotmail is 308kb and Gmail is 309kb, therefore sub 200kb is a good place to be.
///
///     Currently used for with the performance improvement due to this extension noted in brackets:
///         CaseSearch                              (85kb to 53kb, 40% improvement), also removed EnableEventValidation which initially reduced the page size to 69 ...
///         CaseNewStep2 / CaseNew          (97kb to 84kb, 14% improvement),
///         CaseUpdate,                             (120kb to 90kb, 25% improvement)
///         CaseNewStep1 (PNASpecific),      (280kb to 203, 27% improvement)
///         IndividualNew, (PNASpecific)        (281kb to 203 - a 30% improvement - avoiding uploading and downloading the 77kb viewstate each time)
///         IndividualUpdate (PNASpecific)      (283kb to 201 - a 30% improvement - avoiding uploading and downloading the 77kb viewstate each time)
///
///     Some useful webpages detailing average total page sizes of about 1.7mb, with the html component on average about 60kb.
///         http://www.t1shopper.com/tools/calculate/downloadcalculator.php
///
///         http://www.sitepoint.com/average-page-weight-increases-15-2014/
///         http://www.sitepoint.com/average-page-weights-increase-32-2013/
///         http://www.sitepoint.com/best-size-website/
///         http://www.websiteoptimization.com/speed/tweak/average-web-page/
///         http://www.webperformancetoday.com/2013/06/05/web-page-growth-2010-2013/
///         http://www.optimizationweek.com/reviews/average-web-page/
///
///     Note that we are not using this for pages like the CaseView and IndividualView pages as these shouldn't have had a significant viewstate at all.
///     However, there was a decent chunk of ViewState within these pages - and it turns out that this was due to the InfoSplash, MGLLink and CtlSessionExpiryWarning controls
///     We have now set the viewstate of the specific controls within these controls to false, which reduced the IndividualViewPage size from 71kb to 62kb, a 13% reduction.
///     Likewise the CaseView page decreased from 85kb to 69kb (18% improvement)
///
///     Note that for pages like CaseSearch, which does not have an ID, it is ok to use this and parallel case search pages can be opened.
///     This is ok, as no information is compared to / commited to the database, we just update the session search and search results variables.
///
///     Lastly, note that other easy performance improvements to the page size is to make the names of the MasterPage, MainContent, HeaderContent
///     and big user controls like Case and Individual much shorter.  This has a significant impact (e.g. 308 to 280 in IndividualUpdate) as these ids are prefixed to the ids of sub controls...
/// </summary>
public class PagePersistViewStateToFileSystem : MGLBasePage {

    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    //private static string FolderName = "x:/temp/PersistedViewState/PNA";
    private static string FolderName = "x:/temp/PersistedViewState";

    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Does what it says on the tin - appends the siteshortName to the end of the FolderName where we are going to store the view states
    ///     And creates that directory if it does not already exist - to be used in Application_Start
    /// </summary>
    public static void AppendSiteShortNameToFolderName(string siteShortName) {

        try {
            //-----a----- If the root directory for the viewstates does not exist, then lets create it ...
            if (Directory.Exists(FolderName) == false) {
                Directory.CreateDirectory(FolderName);
            }

            //-----b----- Check whether we need to append the siteShortName as a subdirectory - make it happen if we do need to
            if (string.IsNullOrEmpty(siteShortName) == false && string.IsNullOrEmpty(FolderName) == false) {

                if (FolderName.EndsWith(siteShortName) == false) {
                    if (FolderName.EndsWith("/") == false && FolderName.EndsWith("\\") == false) {
                        FolderName = FolderName + "/";
                    }
                    FolderName = FolderName + siteShortName;

                    //-----c----- If the new sub directory does not exist, then lets add it...
                    if (Directory.Exists(FolderName) == false) {
                        Directory.CreateDirectory(FolderName);
                    }
                }
            }
        } catch (Exception ex) {
            Logger.LogError(9, "Could not create the folder to write the persisted view state too.  Has the TrueCrypt folder been closed?  The specific error was: " + ex.ToString());
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    protected override void SavePageStateToPersistenceMedium(object viewState) {
        // serialize the view state into a base-64 encoded string
        LosFormatter los = new LosFormatter();
        StringWriter writer = new StringWriter();
        los.Serialize(writer, viewState);
        // save the string to disk
        StreamWriter sw = File.CreateText(ViewStateFilePath);
        sw.Write(writer.ToString());
        sw.Close();
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    protected override object LoadPageStateFromPersistenceMedium() {
        // determine the file to access
        if (!File.Exists(ViewStateFilePath))
            return null;
        else {
            // open the file
            StreamReader sr = File.OpenText(ViewStateFilePath);
            string viewStateString = sr.ReadToEnd();
            sr.Close();
            // deserialize the string
            LosFormatter los = new LosFormatter();
            return los.Deserialize(viewStateString);
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Combine the folder name with a unique fileName (sessionID, object ID from the URL and the name of the page itself)
    /// </summary>
    public string ViewStateFilePath {
        get {
            // e.g. C:\Dropbox\Dropbox\Data Nirvana\Websites\PNA\PersistedViewState\v200ws4alo2spja1j04xucmq-IndividualUpdate.vs

            StringBuilder fileName = new StringBuilder();
            fileName.Append(Session.SessionID + "_");
            fileName.Append(Path.GetFileNameWithoutExtension(Request.Path).Replace("/", "_"));

            // This is a bit custom, but we also want to include the ID in the fileName as it is possible for multiple pages to be edited at the same time
            // and we dont want the data from different Individuals or Cases to become intermingled!
            string idStr = Request.Params.Get("ID");
            if (string.IsNullOrEmpty(idStr) == false) {
                fileName.Append("_" + idStr);
            }

            fileName.Append(".vs");

            return Path.Combine(FolderName, fileName.ToString());
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Normally called on session end and will clear all the temp view state files relating to the session id provided
    /// </summary>
    public static void ClearAllViewStateFilesInSession(string seshID) {

        // 20-Mar-2016 - Check that the folder name exists - on laptop, truecrypt auto closes when the laptop goes into standby, so this method then crashes
        if (Directory.Exists(FolderName) == true) {
            string[] fileNames = Directory.GetFiles(FolderName);
            if (fileNames != null && fileNames.Length > 0) {
                foreach (string fileName in fileNames) {
                    if (fileName.Contains(seshID) == true) {
                        File.Delete(fileName);
                    }
                }
            }
        }
    }


    //-------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Normally called on application end and will remove all temp view state files within this application's sub folder
    /// </summary>
    public static void ClearAllViewStateFiles() {

        // 20-Mar-2016 - Check that the folder name exists - on laptop, truecrypt auto closes when the laptop goes into standby, so this method then crashes
        if (Directory.Exists(FolderName) == true) {
            string[] fileNames = Directory.GetFiles(FolderName);
            if (fileNames != null && fileNames.Length > 0) {
                foreach (string fileName in fileNames) {
                    File.Delete(fileName);
                }
            }
        }
    }

}
