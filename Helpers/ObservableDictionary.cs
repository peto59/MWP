using System;
using System.Collections.Generic;

namespace MWP.Helpers
{
  
    public class ObservableDictionary<TKey,TValue> : Dictionary<TKey,TValue>
    {
        public Dictionary<TKey,TValue> Items = new Dictionary<TKey,TValue>();
        public ObservableDictionary():base(){}
        ObservableDictionary(int capacity) : base(capacity) { }
        //
        // Do all your implementations here...
        //

        public event EventHandler ValueChanged;

        public void OnValueChanged(Object sender,EventArgs e, TKey key, TValue value)
        {
            EventHandler handler = ValueChanged;
            handler(this, EventArgs.Empty);
        }

        public void AddItem(TKey key, TValue value)
        {
            Items.Add(key, value);
            OnValueChanged(this, EventArgs.Empty, key, value);
            
        }

    
    }
}