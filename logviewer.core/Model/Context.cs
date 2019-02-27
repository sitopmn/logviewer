using logviewer.core;
using MaterialDesignThemes.Wpf;
using System;
using System.Xml.Serialization;

namespace logviewer.Model
{
    /// <summary>
    /// A base class for navigation context
    /// </summary>
    public abstract class Context : NotificationObject, ICloneable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class
        /// </summary>
        /// <param name="icon">Icon of the model</param>
        /// <param name="title">Title of the navigation context</param>
        protected Context(PackIconKind icon, string title)
        {
            Icon = icon;
            Title = title;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class
        /// </summary>
        /// <param name="model">An instance to copy</param>
        protected Context(Context model)
            : this(model.Icon, model.Title)
        { }

        /// <summary>
        /// Gets or sets the title of the navigation context
        /// </summary>
        public virtual string Title
        {
            get => GetValue<string>();
            set => SetValue(value);
        }

        /// <summary>
        /// Gets or sets the icon
        /// </summary>
        [XmlIgnore]
        public virtual PackIconKind Icon { get; private set; }

        /// <summary>
        /// Creates a clone of this object
        /// </summary>
        /// <returns>A clone of this object</returns>
        public abstract object Clone();

        /// <summary>
        /// Checks another object for equality with this instance
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>True if both objects are equal</returns>
        public override bool Equals(object obj)
        {
            return obj is Context model &&
                model.Title == Title;
        }

        /// <summary>
        /// Calculates the hash code of the object
        /// </summary>
        /// <returns>The hash code of the object</returns>
        public override int GetHashCode()
        {
             return Title.GetHashCode();
        }
    }
}
