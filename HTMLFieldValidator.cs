using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using MGL.Data.DataUtilities;


//-----------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MGL.Web.WebUtilities {

    //---------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    ///     Generates the Javascript code and applies it to a client side place holder of your choice so that specific fields can be validated.
    ///     Requires the Javascript Library ValidateHTMLFields.js
    ///     Fields can be named as you like; Each field should have an Error TD or DIV equivalent to it, called exactly the same but with
    ///     "Error" in the name as a suffix (e.g. MyField and MyFieldError ...)
    /// </summary>
    public static class HTMLFieldValidator {

        //--------------------------------------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        ///      This really needs to go in the Web Utilities stuff - so then we would need to change the way that the controls work as well ....
        ///      FieldTypes can be dropdown lists (1) or textboxes (2) ....
        ///      ValidationsToPerform can be
        ///             1 == Not 0,
        ///             2 == Requires a response,
        ///             3 == Combo of either Drop down list or text box is completed,
        ///             4 == Date is valid,
        ///             5 == Date is greater than another date field
        ///             6 == Is a Number,
        ///             7 == Is an email address ....
        ///      FieldNames should be the name of the text box or other data collection widget to be tested ...
        /// </summary>
        public static bool ValidationJavaScriptBuilder(List<string> fieldNames, List<int> fieldTypes, List<int[]> validationsToPerform, out LiteralControl lc) {
            bool success = false;

            lc = new LiteralControl();

            try {

                // build the Javascript ......
                if (fieldNames != null && fieldTypes != null && validationsToPerform != null) {
                    if (fieldNames.Count == fieldTypes.Count && fieldNames.Count == validationsToPerform.Count) {

                        //______Build the JS
                        StringBuilder jsData1 = new StringBuilder();
                        StringBuilder jsData2 = new StringBuilder();
                        StringBuilder jsData3 = new StringBuilder();
                        StringBuilder jsData4 = new StringBuilder();

                        int i = 0;

                        jsData1.Append("var arrayValidationFieldNames = [ ");
                        jsData2.Append("var arrayValidationFieldTypes = [ ");
                        jsData3.Append("var arrayValidationActions = [ ");

                        jsData4.Append("\n\n");
                        jsData4.Append("$(document).ready(function () {");

                        foreach (string fieldName in fieldNames) {
                            if (fieldNames.Count > 1 && i > 0) {
                                jsData1.Append(", ");
                                jsData2.Append(", ");
                                jsData3.Append(", ");
                            }

                            jsData1.Append(DataUtilities.Quote(fieldName));
                            jsData2.Append(fieldTypes[i]);
                            jsData3.Append(DataUtilities.Quote(DataUtilities.GetCSVList(validationsToPerform[i])));

                            // JQuery stuff to set up the on change ....
                            jsData4.Append("$('#" + fieldName + "').change(function () { ChangedField('" + fieldName + "'); });");
                            // Special case - if this is a combo of drop down list and text box, then add the "Other" text box as well to the on change stuff
                            bool isOtherCombo = false;
                            foreach (int tempInt in validationsToPerform[ i ]) {
                                if (tempInt == 3) {
                                    isOtherCombo = true;
                                    break;
                                }
                            }
                            if (isOtherCombo) {
                                jsData4.Append("$('#" + fieldName + "Other').change(function () { ChangedField('" + fieldName + "Other'); });");
                            }


                            i++;
                        }

                        jsData1.Append(" ];");
                        jsData2.Append(" ];");
                        jsData3.Append(" ];");
                        jsData4.Append(" });");

                        //______Assign the JS to the Place Holder ....
                        StringBuilder jsStr = new StringBuilder();
                        jsStr.Append("<script type='text/javascript'>");
                        jsStr.Append(jsData1);
                        jsStr.Append(jsData2);
                        jsStr.Append(jsData3);
                        jsStr.Append(jsData4);
                        jsStr.Append("</script>");
                        lc = new LiteralControl(jsStr.ToString());
//                        JSStuff.Controls.Add(lc);

                    }
                }

                // got to here then looking good
                success = true;

            } catch (Exception ex) {

                Logger.LogError(7, "Problem with the Validation Builder: " + ex.ToString());

            }


            return success;
        }




    }
}
