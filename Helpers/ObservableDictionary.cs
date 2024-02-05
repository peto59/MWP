using System;
using System.Collections.Generic;

namespace MWP
{
  
    public class ObservableDictionary<TKey,TValue> : Dictionary<TKey,TValue>
    {
        public Dictionary<TKey,TValue> Items = new Dictionary<TKey,TValue>();
        public TKey lastValueAdded;
        
        
        public ObservableDictionary() : base()
        {
            
        }
        ObservableDictionary(int capacity) : base(capacity) { }
        //
        // Do all your implementations here...
        //

        public event EventHandler ValueChanged;

        private void OnValueChanged(object sender,EventArgs e, TKey key, TValue value)
        {
            lastValueAdded = key;
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