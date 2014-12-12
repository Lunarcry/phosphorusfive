/*
 * phosphorus five, copyright 2014 - Mother Earth, Jannah, Gaia
 * phosphorus five is licensed as mit, see the enclosed LICENSE file for details
 */

using System;
using System.IO;
using System.Web.UI;
using System.Reflection;
using System.Collections.Generic;
using internals = phosphorus.ajax.core.internals;

namespace phosphorus.ajax.widgets
{
    /// <summary>
    /// general ajax html element
    /// </summary>
    [ViewStateModeById]
    public abstract class Widget : Control, IAttributeAccessor
    {
        /// <summary>
        /// wrapper for an ajax server-side event
        /// </summary>
        public class AjaxEventArgs : EventArgs
        {
            public AjaxEventArgs (string name)
            {
                Name = name;
            }

            /// <summary>
            /// retrieves the name of the event raised
            /// </summary>
            /// <value>the name of the event raised on client side</value>
            public string Name {
                get;
                private set;
            }
        }

        /// <summary>
        /// rules for how to render the tag
        /// </summary>
        public enum RenderingType
        {
            /// <summary>
            /// this is for elements that require both an opening element, and a closing element, such as "div" and "ul"
            /// </summary>
            Default,

            /// <summary>
            /// this forces the element to close itself, even when there is no content, which means it will be rendered with a slash (/) just 
            /// before the greater-than angle-bracket of the opening element. this creates xhtml compliant rendering for you on your page. examples
            /// of element that requires this type of rendering are "input" and "br", but only if you wish to follow xhtml practices
            /// </summary>
            SelfClosing,

            /// <summary>
            /// this is for elements that does not require a closing element. examples of elements that should be rendered
            /// with this type are "p", "li", "input" and "br"
            /// </summary>
            NoClose
        }

        // used to figure out if element just became visible, in-visible, should re-render children, and so on
        protected enum RenderingMode
        {
            Default,
            ReRender,
            ReRenderChildren,
            RenderInvisible
        };

        // contains all attributes of widget
        private internals.AttributeStorage _attributes = new internals.AttributeStorage ();

        // how to render the widget. normally this is automatically determined, but sometimes it needs to be overridden explicitly
        protected RenderingMode _renderMode = RenderingMode.Default;

        /// <summary>
        /// initializes a new instance of the <see cref="phosphorus.ajax.widgets.Widget"/> class
        /// </summary>
        public Widget ()
        { }

        /// <summary>
        /// initializes a new instance of the <see cref="phosphorus.ajax.widgets.Widget"/> class
        /// </summary>
        /// <param name="elementType">html element to render widget with</param>
        public Widget (string elementType)
        {
            ElementType = elementType;
        }

        /// <summary>
        /// initializes a new instance of the <see cref="phosphorus.ajax.widgets.Widget"/> class
        /// </summary>
        /// <param name="elementType">html element to render widget with</param>
        /// <param name="renderType">how to render the widget</param>
        public Widget (string elementType, RenderingType renderType)
            : this (elementType)
        {
            RenderType = renderType;
        }

        /// <summary>
        /// gets or sets the element type used to render the html element such as "p", "div", "ul" etc
        /// </summary>
        /// <value>the tag name</value>
        public virtual string ElementType {
            get { return this ["Tag"]; }
            set {
                if (value.ToLower () != value)
                    throw new ArgumentException ("phosphorus.ajax doesn't like uppercase element names", "value");
                this ["Tag"] = value;
            }
        }

        /// <summary>
        /// gets or sets the tag name used to render the html element when it is invisible. this is sometimes useful since the default
        /// tag rendered when widget is invisible is a span tag, which is not necessarily compatible with the position in the dom you're
        /// rendering it. for instance, if you have a "ul" tag or widget, which has an invisible "li" widget, then rendering a span
        /// tag as a child of a "ul" is illegal according to the html standard. in such circumstances you must change the invisible
        /// tag rendered to become an "li" element
        /// </summary>
        /// <value>the tag name</value>
        public string InvisibleElement {
            get { return ViewState ["ie"] == null ? "span" : ViewState ["ie"] as string; }
            set { ViewState ["ie"] = value; }
        }

        /// <summary>
        /// gets or sets the rendering type of the element, such as whether or not the element is self-closed, has an end element, and so on
        /// </summary>
        /// <value>the rendering type of the element</value>
        public RenderingType RenderType {
            get { return ViewState ["rt"] == null ? RenderingType.Default : (RenderingType)ViewState ["rt"]; }
            set { ViewState ["rt"] = value; }
        }

        /// <summary>
        /// gets or sets the named attribute for the widget. notice that attribute might exist, even if 
        /// return value is null, since attributes can have "null values", such as for instance "controls" 
        /// for the html5 video element, or the "disabled" attribute on form elements. if you wish to 
        /// check for the existence of an attribute, then use <see cref="phosphorus.ajax.widgets.Widget.HasAttribute"/>. 
        /// if you wish to remove an attribute, use the <see cref="phosphorus.ajax.widgets.Widget.RemoveAttribute"/>
        /// </summary>
        /// <param name="name">attribute to retrieve or set</param>
        public virtual string this [string name] {
            get { return _attributes.GetAttribute (name); }
            set {
                if (!IsTrackingViewState) {
                    _attributes.SetAttributePreViewState (name, value);
                } else {
                    _attributes.ChangeAttribute (name, value);
                }
            }
        }

        /// <summary>
        /// determines whether this instance has an attribute aith the specified naame
        /// </summary>
        /// <returns><c>true</c> if this instance has the attribute with the specified name; otherwise, <c>false</c></returns>
        /// <param name="name">name of attribute to check for existence of</param>
        public virtual bool HasAttribute (string name)
        {
            return _attributes.HasAttribute (name);
        }

        /// <summary>
        /// removes an attribute
        /// </summary>
        /// <param name="name">name of attribute to remove</param>
        public virtual void RemoveAttribute (string name)
        {
            _attributes.RemoveAttribute (name);
        }

        /// <summary>
        /// forces a re-rendering of the widget. normally this is not something you should have to mess with yourself, but something the
        /// framework itself will take care of. however, if you wish to force the control to re-render itself entirely as html back to
        /// the client, you can call this method
        /// </summary>
        public void ReRender ()
        {
            _renderMode = RenderingMode.ReRender;
        }

        /// <summary>
        /// forces a re-rendering of the widget's children. normally this is not something you should have to mess with yourself, but 
        /// something the framework itself will take care of. however, if you wish to force the control to re-render its children or content
        /// as html back to the client, you can call this method
        /// </summary>
        public void ReRenderChildren ()
        {
            if (_renderMode != RenderingMode.ReRender)
                _renderMode = RenderingMode.ReRenderChildren;
        }

        /// <summary>
        /// gets a value indicating whether this instance has content or not
        /// </summary>
        /// <value><c>true</c> if this instance has content; otherwise, <c>false</c></value>
        protected abstract bool HasContent {
            get;
        }

        /// <summary>
        /// loads the form data from the http request, override this in your own widgets if you 
        /// have widgets that posts data to the server
        /// </summary>
        protected virtual void LoadFormData ()
        {
            if (this ["disabled"] == null) {
                if (!string.IsNullOrEmpty (this ["name"]) || ElementType == "option") {
                    switch (ElementType.ToLower ()) {
                        case "input":
                            switch (this ["type"]) {
                                case "radio":
                                case "checkbox":
                                    if (Page.Request.Params [this ["name"]] == "on") {
                                        _attributes.SetAttributeFormData ("checked", null);
                                    } else {
                                        _attributes.RemoveAttribute ("checked");
                                    }
                                    break;
                                default:
                                    _attributes.SetAttributeFormData ("value", Page.Request.Params [this ["name"]]);
                                    break;
                            }
                            break;
                        case "textarea":
                            _attributes.SetAttributeFormData ("innerHTML", Page.Request.Params [this ["name"]]);
                            break;
                        case "option":
                            Widget parent = Parent as Widget;
                            if (parent != null && parent.ElementType == "select" && !parent.HasAttribute ("disabled") && !string.IsNullOrEmpty (parent ["name"])) {
                                if (Page.Request.Params [parent ["name"]] == this ["value"]) {
                                    _attributes.SetAttributeFormData ("selected", null);
                                } else {
                                    _attributes.RemoveAttribute ("selected");
                                }
                            }
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// invokes the given event handler. if the widget has an attribute with the 'eventName', then the value
        /// of that attribute will be retrieved, and a method on the page, usercontrol or master page the control belongs 
        /// to will be expected to contains a method with that name. if the widget does not have an attribute with the 
        /// 'eventName' name, then a method with the name of 'eventName' will be invoked searching through the page, 
        /// usercontrol or master page the control belongs to, in that order. all methods invoked this way must have 
        /// the <see cref="phosphorus.ajax.core.WebMethod"/> attribute. if you override this method, please call base 
        /// if you do not recognize the 'eventName'
        /// </summary>
        /// <param name="eventName">event name such as 'onclick', or name of c# method on page, usercontrol or masterpage</param>
        protected virtual void InvokeEventHandler (string eventName)
        {
            string eventHandlerName = eventName; // defaulting to event name for WebMethod invocations from JavaScript
            if (HasAttribute (eventName)) {

                // probably "onclick" or other types of automatically generated mapping between server method and javascript handler
                eventHandlerName = this [eventName];
            }

            // finding out at what context to invoke the method within
            Control owner = this.Parent;
            while (!(owner is UserControl) && !(owner is Page) && !(owner is MasterPage))
                owner = owner.Parent;

            // retrieving the method
            MethodInfo method = owner.GetType ().GetMethod (eventHandlerName, 
                                                            BindingFlags.Instance | 
                                                            BindingFlags.Public | 
                                                            BindingFlags.NonPublic | 
                                                            BindingFlags.FlattenHierarchy);
            if (method == null)
                throw new NotImplementedException ("method + '" + eventHandlerName + "' could not be found");

            // verifying method has the WebMethod attribute
            object[] atrs = method.GetCustomAttributes (typeof (core.WebMethod), false /* for security reasons we want method to be explicitly marked as WebMethod */);
            if (atrs == null || atrs.Length == 0)
                throw new AccessViolationException ("method + '" + eventHandlerName + "' is illegal to invoke over http");

            // invoking methods with the "this" widget and empty event args
            method.Invoke (owner, new object[] { this, new AjaxEventArgs(eventName) });
        }

        /// <summary>
        /// renders all children as json update to be sent back to client. override this one if you wish 
        /// to create custom functionality as an alternative
        /// </summary>
        protected virtual void RenderChildrenWidgetsAsJson (HtmlTextWriter writer)
        {
            // re-rendering all children by default
            (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (ClientID, "innerHTML", GetChildrenHtml ());
        }

        // overridden asp.net properties and methods

        public override bool Visible {
            get {
                return base.Visible;
            }
            set {
                if (!base.Visible && value && IsTrackingViewState && IsPhosphorusRequest) {

                    // this control was made visible during this request and should be rendered as html
                    // unless any of its ancestors are invisible
                    _renderMode = RenderingMode.ReRender;
                } else if (base.Visible && !value && IsTrackingViewState && IsPhosphorusRequest) {

                    // this control was made invisible during this request and should be rendered 
                    // with its invisible html, unless any of its ancestors are invisible
                    _renderMode = RenderingMode.RenderInvisible;
                }
                base.Visible = value;
            }
        }
        
        public override void RenderControl (HtmlTextWriter writer)
        {
            if (AreAncestorsVisible ()) {
                bool ancestorReRendering = IsAncestorRendering ();
                if (Visible) {
                    if (IsPhosphorusRequest && !ancestorReRendering) {
                        if (_renderMode == RenderingMode.ReRender) {

                            // re-rendering entire widget
                            (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (ClientID, "outerHTML", GetWidgetHtml ());
                        } else if (_renderMode == RenderingMode.ReRenderChildren) {

                            // re-rendering all children controls
                            RenderChildrenWidgetsAsJson (writer);
                        } else {

                            // only pass changes back to client as json
                            _attributes.RegisterChanges ((Page as core.IAjaxPage).Manager, ClientID);
                            RenderChildren (writer);
                        }
                    } else {

                        // not ajax request, or ancestors are re-rendering
                        RenderHtmlResponse (writer);
                    }
                } else {

                    // invisible widget
                    if (IsPhosphorusRequest && _renderMode == RenderingMode.RenderInvisible && !ancestorReRendering) {

                        // re-rendering widget's invisible markup
                        (Page as core.IAjaxPage).Manager.RegisterWidgetChanges (ClientID, "outerHTML", GetWidgetInvisibleHtml ());
                    } else if (!IsPhosphorusRequest || ancestorReRendering) {

                        // rendering invisible markup
                        writer.Write (GetWidgetInvisibleHtml ());
                    } // else, nothing to render since widget is in-visible, and this was an ajaxx request
                }
            }
        }

        protected override void LoadViewState (object savedState)
        {
            object[] tmp = savedState as object[];
            base.LoadViewState (tmp [0]);
            _attributes.LoadFromViewState (tmp [1]);
            _attributes.LoadRemovedFromViewState (tmp [2]);
        }

        protected override object SaveViewState ()
        {
            object[] retVal = new object [3];
            retVal [0] = base.SaveViewState ();
            retVal [1] = _attributes.SaveToViewState ();
            retVal [2] = _attributes.SaveRemovedToViewState ();
            return retVal;
        }

        protected override void OnInit (EventArgs e)
        {
            if (Page.IsPostBack)
                LoadFormData ();

            // making sure event handlers are being processed
            if (IsPhosphorusRequest) {

                if (Page.Request.Params ["__pf_widget"] == ClientID) {

                    Page.LoadComplete += delegate {
                        // event was raised for this widget
                        InvokeEventHandler (Page.Request.Params ["__pf_event"]);
                    };
                }
            }

            base.OnInit (e);
        }

        // private methods for internal use in the class

        private string GetWidgetHtml ()
        {
            using (MemoryStream stream = new MemoryStream ()) {
                using (HtmlTextWriter txt = new HtmlTextWriter (new StreamWriter (stream))) {
                    RenderHtmlResponse (txt);
                    txt.Flush ();
                }
                stream.Seek (0, SeekOrigin.Begin);
                using (TextReader reader = new StreamReader (stream)) {
                    return reader.ReadToEnd ();
                }
            }
        }

        private string GetChildrenHtml ()
        {
            using (MemoryStream stream = new MemoryStream ()) {
                using (HtmlTextWriter txt = new HtmlTextWriter (new StreamWriter (stream))) {
                    RenderChildren (txt);
                    txt.Flush ();
                }
                stream.Seek (0, SeekOrigin.Begin);
                using (TextReader reader = new StreamReader (stream)) {
                    return reader.ReadToEnd ();
                }
            }
        }

        private string GetWidgetInvisibleHtml ()
        {
            return string.Format (@"<{0} id=""{1}"" style=""display:none important!;""></{0}>", InvisibleElement, ClientID);
        }

        private bool IsPhosphorusRequest {
            get { return !string.IsNullOrEmpty (Page.Request.Params ["__pf_event"]); }
        }

        private bool AreAncestorsVisible ()
        {
            // if this control and all of its ancestors are visible, then return true, else false
            Control idx = this.Parent;
            while (true) {
                if (!idx.Visible)
                    break;
                idx = idx.Parent;
                if (idx == null) // we traversed all the way up to beyond the Page, and found no in-visible ancestor controls
                    return true;
            }

            // in-visible control among ancestors was found!
            return false;
        }

        private bool IsAncestorRendering ()
        {
            // returns true if any of its ancestors are rendering html
            Control idx = this.Parent;
            while (idx != null) {
                Widget wdg = idx as Widget;
                if (wdg != null && (wdg._renderMode == RenderingMode.ReRender || wdg._renderMode == RenderingMode.ReRenderChildren))
                    return true;
                idx = idx.Parent;
            }
            return false;
        }

        private void RenderHtmlResponse (HtmlTextWriter writer)
        {
            // render opening tag
            writer.Write (string.Format (@"<{0} id=""{1}""", ElementType, ClientID));

            // render attributes
            _attributes.Render (writer);

            if (HasContent) {
                writer.Write (">");
                RenderChildren (writer);
                if (RenderType == RenderingType.Default)
                    writer.Write (string.Format ("</{0}>", ElementType));
            } else {

                // no content in widget
                switch (RenderType) {
                case RenderingType.SelfClosing:
                    writer.Write (" />");
                    break;
                case RenderingType.NoClose:
                    writer.Write (">");
                    break;
                case RenderingType.Default:
                    writer.Write (string.Format ("></{0}>", ElementType));
                    break;
                }
            }
        }

        string IAttributeAccessor.GetAttribute (string key)
        {
            return _attributes.GetAttribute (key);
        }

        void IAttributeAccessor.SetAttribute (string key, string value)
        {
            _attributes.SetAttributePreViewState (key, value);
        }
    }
}

