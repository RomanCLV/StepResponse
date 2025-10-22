using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable enable

namespace StepResponse.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _isDisposed;
        public bool IsDisposed
        {
            get => _isDisposed;
            protected set => _isDisposed = value;
        }

        public ViewModelBase()
        {
            _isDisposed = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raise the <see cref="ViewModelBase.PropertyChanged"/>  event, to refresh all UI controls binded with the given property.
        /// <br />
        /// By default, let it null will call the <seealso cref="CallerMemberNameAttribute"/>.
        /// </summary>
        /// <param name="propertyName">The name of the public property used in binding.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Try to set the value if <paramref name="value"/> is not equal to <paramref name="backingField"/>.
        /// <br />
        /// If yes, it calls the <see cref="ViewModelBase.OnPropertyChanged(string?)"/> to refresh UI.
        /// <br />
        /// By default, <paramref name="propertyName"/> is set to null to call the <seealso cref="CallerMemberNameAttribute"/>.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="backingField">The private property.</param>
        /// <param name="value">The new value to apply.</param>
        /// <param name="propertyName">The name of the public property used in binding.</param>
        /// <returns>Returns true if the values are differents, else returns false.</returns>
        protected bool SetValue<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(backingField, value))
            {
                return false;
            }
            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }
    }
}
