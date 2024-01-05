using System;

namespace MWP.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    public class IntegerChangeEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public int NewValue { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        public IntegerChangeEventArgs(int newValue)
        {
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ObservableInteger
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<IntegerChangeEventArgs>? IntegerChanged;

        private int value;
        /// <summary>
        /// 
        /// </summary>
        public int Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    IntegerChanged?.Invoke(this, new IntegerChangeEventArgs(this.value));
                }
            }
        }
    }
}