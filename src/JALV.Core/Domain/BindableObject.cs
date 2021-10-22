using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace JALV.Core.Domain
{
    /// <summary>
    /// INotifyPropertyChange Implementation
    /// Implements the INotifyPropertyChanged interface and 
    /// exposes a RaisePropertyChanged method for derived 
    /// classes to raise the PropertyChange event. The event 
    /// arguments created by this class are cached to prevent 
    /// managed heap fragmentation.
    /// Refs: http://www.codeproject.com/KB/WPF/WPF_NHibernate_Validator.aspx
    /// </summary>
    [Serializable]
    public abstract class BindableObject
        : DisposableObject, INotifyPropertyChanged
    {
        private static readonly Dictionary<string, PropertyChangedEventArgs> EventArgCache;
        private static readonly object SyncLock = new object();

        #region Constructors

        public BindableObject()
        {
            IsPropertyChangedEventEnabled = true;
        }

        static BindableObject()
        {
            EventArgCache = new Dictionary<string, PropertyChangedEventArgs>();
        }

        #endregion // Constructors

        #region Pattern Observable

        /// <summary>
        /// Raised when a public property of this object is set.
        /// </summary>
        [field: NonSerialized]
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Indicates whether ownership change notification is enabled
        /// </summary>
        public virtual bool IsPropertyChangedEventEnabled { get; set; }

        /// <summary>
        /// Returns an instance of PropertyChangedEventArgs for 
        /// the specified property name.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property to create event args for.
        /// </param>		
        public static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentException("propertyName cannot be null or empty.");

            PropertyChangedEventArgs args;
            lock (SyncLock)
            {
                if (!EventArgCache.TryGetValue(propertyName, out args))
                {
                    EventArgCache.Add(propertyName, args = new PropertyChangedEventArgs(propertyName));
                }
            }

            return args;
        }

        /// <summary>
        /// Attempts to raise the PropertyChanged event, and 
        /// invokes the virtual AfterPropertyChanged method, 
        /// regardless of whether the event was raised or not.
        /// </summary>
        /// <param name="propertyName">
        /// The property which was changed.
        /// </param>
        public virtual void RaisePropertyChanged(string propertyName)
        {
            // If ownership change notification is not enabled then I do nothing
            if (!IsPropertyChangedEventEnabled)
                return;

            VerifyProperty(propertyName);

            var handler = PropertyChanged;
            if (handler != null)
            {
                //Get the cached event args.
                var args =
                    GetPropertyChangedEventArgs(propertyName);

                // Raise the PropertyChanged event.
                handler(this, args);
            }

            OnAfterPropertyChanged(propertyName);
        }

        /// <summary>
        /// Derived classes can override this method to
        /// execute logic after a property is set. The 
        /// base implementation does nothing.
        /// </summary>
        /// <param name="propertyName">
        /// The property which was changed.
        /// </param>
        protected virtual void OnAfterPropertyChanged(string propertyName)
        {
        }

        #endregion

        #region Helpers

        [Conditional("DEBUG")]
        private void VerifyProperty(string propertyName)
        {
            if (propertyName.IndexOf(".", StringComparison.Ordinal) >= 0)
                return;

            // Thanks to Rama Krishna Vavilala for the tip to use TypeDescriptor here, instead of manual
            // reflection, so that custom properties are honored too.
            // http://www.codeproject.com/KB/WPF/podder1.aspx?msg=2381272#xx2381272xx

            var propertyExists = TypeDescriptor.GetProperties(this).Find(propertyName, false) != null;
            if (!propertyExists)
            {
                // The property could not be found,
                // so alert the developer of the problem.

                var msg = $"{propertyName} is not a public property of {GetType().FullName}";

                Debug.Fail(msg);
            }
        }

        #endregion
    }
}