using System;
using System.Collections.Generic;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using MGL.Data.DataUtilities;
using MGL.Security;
using MGL.DomainModel.HumanitarianActivities;

//---------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Web.WebUtilities {
    //------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Started in 2008!!!
    ///     
    /// </summary>
    public class HTMLUtilities {

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static string DefaultPath = "~/";

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static string ImageRootDirectory = "images/";


        //-------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - This method builds the html to include static resources like javascript and css files.
        ///     
        ///     We want to move towards retreiving all the JS and Styles from a static resource domain e.g. static.datanirvana.org
        ///     To do this is fine, but we also want to be able to test using local resources, so lets setup a web config param specifying the static resource path
        ///     ~/ is the local path and e.g. static.datanirvana.org would be the static path
        ///     If an external resource we need to choose https or not depending on the current session ...
        ///     THe resource list will appear like this: "Scripts/jquery-1.11.3.min.js", "Scripts/Site.js", "Styles/Site.css" 
        ///     The Scripts or Styles prefix is critical as this determines which type of resource is referenced.
        ///     Note that for external resources like maps.googleapi.com still DO NOT include the http / https prefix ...
        /// </summary>
        public static string BuildStaticResourcesHTML(Page currentPage, string[] resourceNames) {

            StringBuilder str = new StringBuilder();

            if (resourceNames != null) {
                //-----a1----- declare all the variables to building these static resources to improve the readability!
                string staticResourcePath = MGLApplicationInterface.Instance().StaticResourcePath;
                bool minifyJS = MGLApplicationInterface.Instance().StaticJavascriptIsMinified;
                string httpPrefix = (MGLSessionInterface.Instance().UseHTTPS == true) ? "https://" : "http://";

                //-----a2----- 11-May-2016 - Ok so here, if the SSLError bool in the session is set, then we DONT want to use the external resources as they wont work, so reset to use the local variants
                if (MGLSessionInterface.Instance().SSLError == true) {
                    staticResourcePath = "~/";
                    int userID = (Authorisation.CurrentUser != null) ? Authorisation.CurrentUser.ID : 0;
                    // This may be a little verbose on some sites, but lets keep it in for now to see how often this occurs....
                    Logger.LogWarning( "SSL configuration error detected for user "+userID+" with ipaddress "+IPAddressHelper.GetIPAddressFromHTTPRequest()+" on page '"+currentPage.Title+"'." );
                }
                
                //-----a3---- And double check if we need to add a trailing forward slash ....
                string dir = (staticResourcePath.EndsWith("/") == true) ? "" : "/";

                //-----a4----- 31-Mar-2016 - lets append the JSVersion to ALL static resources, so that we can try to influence clients browsers to automatically refresh after updates
                // without having to visit the new DefaultFullReload page - this needs testing!!
                // http://stackoverflow.com/questions/32414/how-can-i-force-clients-to-refresh-javascript-files#32427
                // lets also remove the . from the version just to make 100% sure we dont confuse any legacy code that splits the file suffix on the last dot (oooops!)
                string versionSuffix = "?v=" + MGLApplicationInterface.Instance().JSVersion.Replace( ".", "" );


                //-----b----- loop through the static resources
                foreach (string staticResource in resourceNames) {
                    //-----c----- Get the relative path to this resource
                    string tempPath = httpPrefix + staticResourcePath + dir + staticResource;
                    // 31-Mar-2016 - special case for script files that are built dynamically and accessed from the code directory
                    // We dont want to get these from the remote resource location, these should always be local....
                    // Note that we cannot use the default path as this gets reset to the full default path - we always just want to use the simple ~/
                    if (staticResource.StartsWith("Code", StringComparison.CurrentCultureIgnoreCase) == true) {
                        tempPath = currentPage.ResolveClientUrl("~/" + staticResource); 
                    }
                    // And lets resolve the right relative URL if this is a local implementation
                    if (staticResourcePath.StartsWith("~")) {
                        tempPath = currentPage.ResolveClientUrl(staticResourcePath + staticResource);
                    }

                    //-----d----- Build the resource tag!
                    // 31-Mar-2016 - also include the code prefix here so that the semi-static files for the Information and Map pages are treated in the same way with the v suffix
                    if (staticResource.StartsWith("Scripts", StringComparison.CurrentCultureIgnoreCase) == true
                        || staticResource.StartsWith("Code", StringComparison.CurrentCultureIgnoreCase) == true) {

                        //-----e----- Determine whether or not the javascript needs to be minified - if it does then convert the JS at the end to be .min.js if it has not already been set
                        if (minifyJS == true && tempPath.EndsWith(".min.js", StringComparison.CurrentCultureIgnoreCase) == false) {
                            tempPath = tempPath.ToLower().Replace(".js", ".min.js");
                        }
                        str.Append("<script src=\"" + tempPath + versionSuffix + "\" type=\"text/javascript\"></script>");

                    } else if (staticResource.StartsWith("Styles", StringComparison.CurrentCultureIgnoreCase)) {
                        //-----f1----- 19-Apr-2016 - we are now also minifying the CSS too - if it does then convert the css at the end to be .min.css if it has not already been set
                        if (minifyJS == true && tempPath.EndsWith(".min.css", StringComparison.CurrentCultureIgnoreCase) == false) {
                            tempPath = tempPath.ToLower().Replace(".css", ".min.css");
                        }                       
                        //-----f2----- Build the styles link!
                        str.Append("<link href=\"" + tempPath + versionSuffix + "\" rel=\"stylesheet\" type=\"text/css\" />");

                    } else {
                        // Unusual / unknown static resource ... In fact, most probably an external resource!  So lets not amend the staticResource string, we just prefix and dont add the ?v=
                        // To reiterate - DO prefix with http / https and DO NOT add the version as a parameter ...
                        // e.g. maps.googleapis.com/maps/api/js?key=MY_API_KEY
                        str.Append("<script src=\"" + httpPrefix + staticResource + "\" type=\"text/javascript\"></script>");
                    }
                }
            }

            return str.ToString();
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     Given an image path (../../images/MyPNG.png), returns the file name (MyPNG.png)
        /// </summary>
        public static string GetFileNameFromPath(string filePath) {
            string fileName = "";

            if (filePath != null && filePath != "") {
                string[] bits = filePath.Split('/', '\\');
                if (bits != null && bits.Length > 0) {
                    fileName = bits[bits.Length - 1];
                }
            }
            return fileName;
        }


        ////--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     If the hover URL is null the method assumes that the image file has the same name, but with the _hover suffix
        ///     18-Mar-2016 - cleaned this up and reduced the hover name from _hover to H to reduce the download sizes...
        ///     Note that this does NOT cope with images with "." in them!!!!
        /// </summary>
        public static void AddMouseOver(ImageButton myButton, string srcImagePath, string hoverImageURL) {
            myButton.Attributes.Add("onmouseout", "this.src='" + myButton.ImageUrl + "'");

            // if the hover URL is null assume that the name is the same, but with _hover as a suffix
            if (hoverImageURL == null || hoverImageURL == "") {
                try {
                    string imgFileName = GetFileNameFromPath(myButton.ImageUrl);
                    string[] bits = imgFileName.Split('.');
//                    hoverImageURL = srcImagePath + bits[0] + "_hover." + bits[1];
                    hoverImageURL = srcImagePath + bits[0] + "H." + bits[1];
                } catch { }
            }

            if (hoverImageURL != null && hoverImageURL != "") {
                myButton.Attributes.Add("onmouseover", "this.src='" + hoverImageURL + "'");
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        /// </summary>
        public static string GetImageDirectoryPath(string relativePathToAppRoot) {

            relativePathToAppRoot = ValidatePath(relativePathToAppRoot);

            string srcImgPath = null;
//            if (relativePathToAppRoot == null || relativePathToAppRoot == "" || (relativePathToAppRoot.Contains( "~" ) && ignoreTildas==false)) {
            if (relativePathToAppRoot == null || relativePathToAppRoot == "") {
                srcImgPath = ImageRootDirectory;
            } else {
                srcImgPath = relativePathToAppRoot + ImageRootDirectory;
            }
            return srcImgPath;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     If the hover URL is null the method assumes that the image file has the same name, but with the _hover suffix
        ///     18-Mar-2016 - cleaned this up and reduced the hover name from _hover to H to reduce the download sizes...
        ///     Note that this does NOT cope with images with "." in them!!!!
        /// </summary>
        public static string GetHoverImageName(string imgURL) {
            string hoverImgURL = null;
            if (imgURL != null && imgURL != "") {
                try {
                    string imgFileName = GetFileNameFromPath(imgURL);
                    string[] bits = imgFileName.Split('.');
                    // 18-Mar-2016 - what about if the image name has dots in it?  This is not caught here ...
                    //hoverImgURL = imgURL.Replace(imgFileName, bits[0] + "_hover." + bits[1]);
                    hoverImgURL = imgURL.Replace(imgFileName, bits[0] + "H." + bits[1]);
                } catch { }
            }
            return hoverImgURL;
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     If the hover URL is null the method assumes that the image file has the same name, but with the _hover suffix
        /// </summary>
        public static string ValidatePath( string urlPath ) {
            if ( urlPath != null && urlPath != "" ) {
                if (urlPath.EndsWith("/") == false && urlPath.EndsWith("\\") == false) {
                    urlPath = urlPath + "/";
                }
            }
            return urlPath;
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     Probably used for a link button containing an image ... The image file name should be relative to the root of the images directory
        /// </summary>
        public static void AddMouseOverEvents(HtmlAnchor myButt, string imageID, string srcImgPath, string imageFileName) {
            AddMouseOverEvents( myButt.Attributes, imageID, srcImgPath, imageFileName );
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        /// </summary>
        public static void AddMouseOverEvents(LinkButton myButt, string imageID, string srcImgPath, string imageFileName) {
            AddMouseOverEvents(myButt.Attributes, imageID, srcImgPath, imageFileName);
        }
        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        /// </summary>
        public static void AddMouseOverEvents(AttributeCollection atts, string imageID, string srcImgPath, string imageFileName) {

            // check for tildas and default if found them
            if (srcImgPath != null && srcImgPath.Contains("~")) {
                srcImgPath = ImageRootDirectory;
            }

            // see if there is an attribute there already - if so, then append it
            if (atts["onmouseout"] != null && atts["onmouseout"] != "") {
                atts["onmouseout"] = atts["onmouseout"] + "document.getElementById('" + imageID + "').src='" + srcImgPath + imageFileName + "';";
            } else {
                atts.Add("onmouseout", "javascript:document.getElementById('" + imageID + "').src='" + srcImgPath + imageFileName + "';");
            }

            if (atts["onmouseover"] != null && atts["onmouseover"] != "") {
                atts["onmouseover"] = atts["onmouseover"] + "document.getElementById('" + imageID + "').src='" + srcImgPath + HTMLUtilities.GetHoverImageName(imageFileName) + "';";
            } else {
                atts.Add("onmouseover", "javascript:document.getElementById('" + imageID + "').src='" + srcImgPath + HTMLUtilities.GetHoverImageName(imageFileName) + "';");
            }
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     If you want to use the default hover image, set the jsIm to be null
        /// </summary>
        public static string GetLiteralMGLLink(
            string linkID, string imID, string imPath, string im, string linkText, string linkAction, bool doHoverOver, string jsIm, string toolTip) {

            return GetLiteralMGLLink(linkID, imID, imPath, im, linkText, linkAction, doHoverOver, jsIm, toolTip, false, false, 0, null);
        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        ///     If you want to use the default hover image, set the jsIm to be null
        /// </summary>
        public static string GetLiteralMGLLink(
            string linkID, string imID, string imPath, string im, string linkText, string linkAction, bool doHoverOver, string jsIm,
            string toolTip, bool padImageAndText, bool textBeforeIm, int textPixelAdjustment, string cssClass) {

            StringBuilder str = new StringBuilder();

            str.Append("<a id='"+linkID+"' href=\"" + linkAction + "\" ");

            if (cssClass != null && cssClass != "") {
                str.Append("class=\"" + cssClass + "\" ");
            }

            // only add the a tooltip if there is more than just an image
            if (toolTip != null && toolTip != "") {
                str.Append("title=\"" + toolTip + "\" " );
            }


            string padClass = (padImageAndText) ? "" : "class=\"IA2\"";
            string pixAdj = "";
            if (textPixelAdjustment != 0) {
                pixAdj = "style='position:relative;top:"+textPixelAdjustment+"px;'";
            }

            if (imPath != null && imPath != "" && im != null && im != "") {

                if (doHoverOver) {
                    if (jsIm != null && jsIm != "") {
                        str.Append("onmouseover=\"swapIm('" + imID + "',"+jsIm+"H);\"");
                        str.Append("onmouseout=\"swapIm('" + imID + "'," + jsIm + ");\"");
                    } else {
                        str.Append("onmouseover=\"document.getElementById('" + imID + "').src='" + imPath + HTMLUtilities.GetHoverImageName(im) + "';\"");
                        str.Append("onmouseout=\"document.getElementById('" + imID + "').src='" + imPath + im + "';\"");
                    }
                }
                str.Append(">");

                if (textBeforeIm) {
                    if (linkText != null && linkText != "") {
                        str.Append("<span " + padClass + " "+pixAdj+">" + linkText + "</span>");
                    }
                }

                str.Append("<span "+padClass+"><img id='" + imID + "' border='0' title=\"" + toolTip + "\" src='" + imPath + im + "' /></span>");

            } else {
                str.Append(">");
            }
            if (textBeforeIm == false) {
                if (linkText != null && linkText != "") {
                    str.Append("<span " + padClass + " "+pixAdj+">" + linkText + "</span>");
                }
            }
            str.Append("</a>");

            return str.ToString();
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     22-Mar-2016 - DEPRECATED - Checked and no longer used ...
        /// </summary>
        public static string AbsoluteCodeRoot(string hostName, string appName) {
            StringBuilder root = new StringBuilder();

            if (hostName != null && hostName != "") {
                root.Append( "http://" + hostName );
            }

            if (appName != null && appName != "") {
                root.Append( appName );
            }

            if ( root.Length == 0) {
                root.Append( "~/" );
            } else if (root.ToString().EndsWith("/") == false) {
                root.Append("/");
            }

            root.Append( "code/" );

            return root.ToString();
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateDropDownList(DropDownList ddl, List<KeyValuePair<string, string>> list, bool addPleaseChoose) {

            List<ListItem> lis = new List<ListItem>();
            //lis.Add(new ListItem("Please choose", ""));

            if ( addPleaseChoose ) {
                lis.Add(new ListItem("Please choose", ""));
            }

            foreach (KeyValuePair<string, string> kvp in list) {
                lis.Add(new ListItem(kvp.Value, kvp.Key));
            }

            PopulateDropDownList(ddl, lis);

        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateDropDownList(DropDownList ddl, List<KeyValuePair<int, string>> list, bool addPleaseChoose) {

            List<ListItem> lis = new List<ListItem>();
            //lis.Add(new ListItem("Please choose", ""));

            if (addPleaseChoose) {
                lis.Add(new ListItem("Please choose", "")); // Please choose
                lis[0].Enabled = false;
            }

            foreach (KeyValuePair<int, string> kvp in list) {
                lis.Add(new ListItem(kvp.Value, kvp.Key.ToString()));
            }

            PopulateDropDownList(ddl, lis);

        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateDropDownList(DropDownList ddl, Dictionary<int,Organisation> orgList, bool addPleaseChoose) {

            List<ListItem> lis = new List<ListItem>();
            //lis.Add(new ListItem("Please choose", ""));

            if (addPleaseChoose) {
                lis.Add(new ListItem("Please choose", ""));
                lis[0].Enabled = false;
            }

            foreach (Organisation org in orgList.Values) {
                lis.Add(new ListItem(org.OrganisationAcronym, org.ID.ToString()));
            }

            PopulateDropDownList(ddl, lis);

        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateDropDownList(DropDownList ddl, List<ListItem> list) {

            ddl.DataSource = list;
            ddl.DataTextField = "Text";
            ddl.DataValueField = "Value";
            ddl.DataBind();
        }



        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateListBox(ListBox lb, List<KeyValuePair<string,string>> list) {

            List<ListItem> lis = new List<ListItem>();
            //lis.Add(new ListItem("Please choose", ""));

            foreach (KeyValuePair<string, string> kvp in list) {
                lis.Add(new ListItem(kvp.Value, kvp.Key));
            }

            PopulateListBox(lb, lis);

        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateListBox(ListBox lb, List<KeyValuePair<int, string>> list) {

            List<ListItem> lis = new List<ListItem>();
            //lis.Add(new ListItem("Please choose", ""));

            foreach (KeyValuePair<int, string> kvp in list) {
                lis.Add(new ListItem(kvp.Value, kvp.Key.ToString()));
            }

            PopulateListBox(lb, lis);                       

        }
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static void PopulateListBox(ListBox lb, List<ListItem> list) {

            lb.DataSource = list;
            lb.DataTextField = "Text";
            lb.DataValueField = "Value";
            lb.DataBind();
        }


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Uses streams to render HTML controls and get the html code generated
        /// </summary>
        /// <param name="ControlToRender"></param>
        /// <returns></returns>
        public static string RenderControlToHtml(Control ControlToRender) {

            StringBuilder sb = new StringBuilder();

            try {

                System.IO.StringWriter stWriter = new System.IO.StringWriter(sb);
                System.Web.UI.HtmlTextWriter htmlWriter = new System.Web.UI.HtmlTextWriter(stWriter);
                ControlToRender.RenderControl(htmlWriter);

                stWriter.Dispose();
                htmlWriter.Dispose();

            } catch (Exception ex) {
                Logger.Log("Problem rendering a control to html in HTMLUtilities.RenderControlToHtml: " + ex.ToString());
            }
            
            return sb.ToString();
        }

    }
}