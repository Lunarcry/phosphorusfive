
/*
 * phosphorus five, copyright 2014 - Mother Earth, Jannah, Gaia
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System;
using System.Configuration;
using System.Collections.Generic;
using phosphorus.core;
using phosphorus.expressions;

namespace phosphorus.data
{
    /// <summary>
    /// class wrapping [pf.data.update] and its associated supporting methods
    /// </summary>
    public static class data_update
    {
        /// <summary>
        /// updates the results of the given expression in database, either according to a static [soure] node,
        /// or a relative [rel-source] node. if you supply a static [source], then source can either be a constant
        /// value, or an expression. if you supply a [rel-source], then source must be relative to nodes you wish
        /// to update
        /// </summary>
        /// <param name="context"><see cref="phosphorus.Core.ApplicationContext"/> for Active Event</param>
        /// <param name="e">parameters passed into Active Event</param>
        [ActiveEvent (Name = "pf.data.update")]
        private static void pf_data_update (ApplicationContext context, ActiveEventArgs e)
        {
            // making sure database is initialized
            data_common.Initialize (context);

            // figuring out source, and executing the corresponding logic
            if (e.Args.Count > 0 && e.Args.LastChild.Name == "rel-source") {

                // static source, not a node, might be an expression
                UpdateRelativeSource (e.Args, context);
            } else {

                // relative source, source must be an expression
                UpdateStaticSource (e.Args, context);
            }
        }
        
        /*
         * sets all destination nodes relative to themselves
         */
        private static void UpdateRelativeSource (Node node, ApplicationContext context)
        {
            // iterating through all destinations, figuring out source relative to each destinations
            List<Node> changed = new List<Node> ();
            foreach (var idxDestination in XUtil.Iterate (node, data_common.Database, context)) {
                
                // figuring out which file Node updated belongs to, and storing in changed list
                data_common.AddNodeToChanges (idxDestination.Node, changed);

                // source is relative to destination
                idxDestination.Value = XUtil.Single<object> (node.LastChild, idxDestination.Node, context, null);
            }

            // saving all affected files
            data_common.SaveAffectedFiles (context, changed);
        }

        /*
         * sets all destinations to static value where value is string or expression
         */
        private static void UpdateStaticSource (Node node, ApplicationContext context)
        {
            // figuring out source
            object source = GetStaticSource (node, context);

            // iterating through all destinations, updating with source
            List<Node> changed = new List<Node> ();
            foreach (var idxDestination in XUtil.Iterate (node, data_common.Database, context)) {
                
                // figuring out which file Node updated belongs to, and storing in changed list
                data_common.AddNodeToChanges (idxDestination.Node, changed);

                // doing actual update
                idxDestination.Value = source;
            }

            // saving all affected files
            data_common.SaveAffectedFiles (context, changed);
        }

        /*
         * retrieves the source for a "static source" update operation
         */
        private static object GetStaticSource (Node node, ApplicationContext context)
        {
            object retVal = null;

            // checking to see if there is a source at all
            if (node.LastChild.Name == "source") {

                // we have source nodes
                if (node.LastChild.Value != null) {

                    // source is either constant value or an expression
                    retVal = XUtil.Single<object> (node.LastChild, context, null);
                } else {

                    // source is either a node or null
                    if (node.LastChild.Count == 1) {

                        // source is a node
                        retVal = node.LastChild.FirstChild;
                    } else if (node.LastChild.Count == 0) {

                        // source is null
                        retVal = null;
                    } else {

                        // more than one source
                        throw new ArgumentException ("[pf.data.update] requires that you give it only one source");
                    }
                }
            }

            // returning source (or null) back to caller
            return retVal;
        }
    }
}
