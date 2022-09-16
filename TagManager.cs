using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagLib;
namespace Ass_Pain
{
    internal class TagManager
    {
        TagLib.File tfile;

        public TagManager(string file)
        {
            tfile = TagLib.File.Create(file);
        }

        public string Title()
        {
            return tfile.Tag.Title;
        }

        public void Title(string update)
        {
            if(update != Title())
            {
                tfile.Tag.Title = update;
            }
        }

        ~TagManager()
        {
            tfile.Save();
        }
    }
}