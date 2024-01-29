using AndroidX.Fragment.App;
using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Android.Content.Res;
using Android.Graphics;
using Android.Net;
using Fragment = AndroidX.Fragment.App.Fragment;
using MWP.BackEnd;
using MWP.Helpers;
using Color = Android.Graphics.Color;
using Orientation = Android.Widget.Orientation;

namespace MWP
{
    /// <inheritdoc />
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
    public class SettingsFragment : Fragment
    {
        private readonly Context context;
        private ScrollView? mainLayout;
        private AssetManager? assets;
        private LinearLayout? mainLinearLayout;
        private Typeface? font;


        /// <summary>
        /// Incilalizacia zakladnych datovych poloziek fragmentu
        /// </summary>
        /// <param name="ctx">Context ziskany z aktivity, v tomto pripade z hlavnej aktivity</param>
        /// <param name="assets">Asset ziskane z hlavnej aktivity, sluzia na pristup k statickym suborom aplikacie</param>
        public SettingsFragment(Context ctx, AssetManager? assets)
        {
            context = ctx;
            this.assets = assets;
            font = Typeface.CreateFromAsset(assets, "sulphur.ttf");
        }


        /// <inheritdoc />
        public override View? OnCreateView(LayoutInflater inflater, ViewGroup? container, Bundle? savedInstanceState)
        {
            View? view = inflater.Inflate(Resource.Layout.settings_fragment, container, false);

            mainLayout = view?.FindViewById<ScrollView>(Resource.Id.settings_fragment_layout);
            mainLinearLayout = view?.FindViewById<LinearLayout>(Resource.Id.settings_main_linear);

            TextView? dropdownItem = view?.FindViewById<TextView>(Resource.Id.dropdown_item_textview);
            if (dropdownItem != null) dropdownItem.Typeface = font;

            /*
             * INT  -> popup list
             * BOOL -> switch
             */
            List<(string name, Func<bool> read, Action<bool> write, string? remark)> boolSettings = SettingsManager.GetBoolSettings();
            List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> intSettings = SettingsManager.GetIntSettings();

            for (int i = 0; i < boolSettings.Count; i++)
            {
                mainLinearLayout?.AddView(
                    SwitchSetting(
                        boolSettings[i].name, 
                        boolSettings[i].read(), 
                        boolSettings[i].remark, 
                        boolSettings[i].write
                    )
                );
            }
            
            for (int i = 0; i < intSettings.Count; i++)
            {
                mainLinearLayout?.AddView(
                    DropdownSetting(
                        intSettings[i].name,
                        intSettings[i].read(),
                        intSettings[i].remark,
                        intSettings[i].write,
                        intSettings[i].mapping
                    )
                );
            }
            
            
    
            return view;
        }


        /*
         * Metoda sluziaca na vygenerovanie elementov uzivatelskeho prostredia pre nastavenie typu Bool, cize
         * nastavenie pri ktorom sa meni hodnota z 0 na 1, inak povedane switch.
         * Vracia LinearLayout ktory je potrebne vlozit na hlavneho LinearLayoutu fragmentu
         *
         * Metoda prijma nazov nastavenia, aktualnu hodnotu, text komentara a akciu sluziaca
         * na zapisanie novej hodnoty v pripade jej zmeny
         */
        private LinearLayout SwitchSetting(string name, bool initialValue, string? remark, Action<bool> write)
        {
            /*
             * Vytvorenie vonkajsieho layoutu v ktorom sa v riadku bude nachadzat aj texty a aj switch.
             * Orientacia je horizontalna kedze budu v riadku
             */
            LinearLayout linOuter = new LinearLayout(context);
            linOuter.Orientation = Orientation.Horizontal;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            linParams.SetMargins(
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f)
            );
            linOuter.LayoutParameters = linParams;
            linOuter.SetGravity(GravityFlags.Center);

            /*
             * Vytvorenie vnutorneho layoutu pre TextViews,
             * Orientacia je vertikalna kedze jeden text je hlavny pre nazov nastavenia
             * druhy text sluzi na komentar pre pouzivatela k nastaveniu
             */
            LinearLayout linTexts = new LinearLayout(context);
            linTexts.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linTextsParams = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent
            );
            linTextsParams.Weight = 1;
            linTexts.LayoutParameters = linTextsParams;
            
            /*
             * Vytvorenie jednotlivych TextView elementov pre nazov nastavenia a komentar k nastaveniu
             */
            LinearLayout.LayoutParams textViewParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            
            TextView settingName = new TextView(context);
            settingName.Text = name;
            settingName.Typeface = font;
            settingName.SetTextColor(Color.White);
            settingName.TextSize = 20;
            settingName.LayoutParameters = textViewParams;
            
            TextView settingRemark = new TextView(context);
            settingRemark.Text = remark;
            settingRemark.Typeface = font;
            settingRemark.SetTextColor(Color.ParseColor("#A9A9A9"));
            settingRemark.TextSize = 12;
            settingRemark.LayoutParameters = textViewParams;
            
            /*
             * Pridavanie TextView elementov do LinearLayoutu s vertikalnou orientaciou 
             */
            linTexts.AddView(settingName);
            linTexts.AddView(settingRemark);

            linOuter.AddView(linTexts);
            linOuter.AddView(CreateSwitch(write, initialValue));
            
            return linOuter;

        }
        
         /*
         * Metoda sluziaca na vygenerovanie elementov uzivatelskeho prostredia pre nastavenie typu Int, cize
         * nastavenie pri ktorom viacero ciselnych hodnot ma pridelene iste string hodnoty, co si vyzaduje rolovaci vyber moznosti.
         * Vracia LinearLayout ktory je potrebne vlozit do hlavneho LinearLayoutu fragmentu
         *
         * Metoda prijma nazov nastavenia, aktualnu hodnotu, text komentara a akciu sluziaca
         * na zapisanie novej hodnoty v pripade jej zmeny
         */
        private LinearLayout DropdownSetting(string name, int initialValue, string? remark, Action<int> write, Dictionary<string, int>? mapping)
        {
            /*
             * Vytvorenie vonkajsieho layoutu v ktorom sa v riadku bude nachadzat aj texty a aj dropdown.
             * Orientacia je horizontalna kedze budu v riadku
             */
            LinearLayout linOuter = new LinearLayout(context);
            linOuter.Orientation = Orientation.Horizontal;
            LinearLayout.LayoutParams linParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent
            );
            linParams.SetMargins(
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f), 
                (int)ConvertDpToPixels(20.0f)
            );
            linOuter.LayoutParameters = linParams;
            linOuter.SetGravity(GravityFlags.Center);

            /*
             * Vytvorenie vnutorneho layoutu pre TextViews,
             * Orientacia je vertikalna kedze jeden text je hlavny pre nazov nastavenia
             * druhy text sluzi na komentar pre pouzivatela k nastaveniu
             */
            LinearLayout linTexts = new LinearLayout(context);
            linTexts.Orientation = Orientation.Vertical;
            LinearLayout.LayoutParams linTextsParams = new LinearLayout.LayoutParams(
                0,
                ViewGroup.LayoutParams.WrapContent
            );
            linTextsParams.Weight = 1;
            linTexts.LayoutParameters = linTextsParams;
            
            /*
             * Vytvorenie jednotlivych TextView elementov pre nazov nastavenia a komentar k nastaveniu
             */
            LinearLayout.LayoutParams textViewParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            
            TextView settingName = new TextView(context);
            settingName.Text = name;
            settingName.Typeface = font;
            settingName.SetTextColor(Color.White);
            settingName.TextSize = 20;
            settingName.LayoutParameters = textViewParams;
            
            TextView settingRemark = new TextView(context);
            settingRemark.Text = remark;
            settingRemark.Typeface = font;
            settingRemark.SetTextColor(Color.ParseColor("#A9A9A9"));
            settingRemark.TextSize = 12;
            settingRemark.LayoutParameters = textViewParams;
            
            /*
             * Pridavanie TextView elementov do LinearLayoutu s vertikalnou orientaciou 
             */
            linTexts.AddView(settingName);
            linTexts.AddView(settingRemark);

            linOuter.AddView(linTexts);
            linOuter.AddView(CreateDropdown(write, initialValue, mapping));
            
            return linOuter;

        }


        
        private float ConvertDpToPixels(float dpValue) {
            if (context.Resources is not { DisplayMetrics: not null }) return 0.0f;
            var screenPixelDensity = context.Resources.DisplayMetrics.Density;
            var pixels = dpValue * screenPixelDensity;
            return pixels;

        } 
        
        
        private AndroidX.AppCompat.Widget.SwitchCompat CreateSwitch(Action<bool> write, bool initialValue)
        {
            AndroidX.AppCompat.Widget.SwitchCompat switchCompat = new AndroidX.AppCompat.Widget.SwitchCompat(context);
            LinearLayout.LayoutParams switchParams = new LinearLayout.LayoutParams(
                (int)ConvertDpToPixels(60.0f),
                (int)ConvertDpToPixels(50.0f)
            );
            switchParams.SetMargins(0, 0, 0, 0);
            switchCompat.LayoutParameters = switchParams;
            switchCompat.TextSize = 10;
            switchCompat.SetTrackResource(Resource.Drawable.custom_track);
            switchCompat.SetThumbResource(Resource.Drawable.custom_thumb);

            switchCompat.Checked = initialValue;
            switchCompat.CheckedChange += (sender, args) => write(args.IsChecked);
            
            
            return switchCompat;
        }

        private Spinner CreateDropdown(Action<int> write, int initialValue, Dictionary<string, int>? values)
        {
            string[]? items = values?.Keys.ToArray();
            
            LinearLayout.LayoutParams dropdownParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent
            );
            
            Spinner dropdown = new Spinner(context);
            dropdown.LayoutParameters = dropdownParams;
            dropdown.SetBackgroundResource(Resource.Drawable.rounded_dark);
            dropdown.SetPadding((int)ConvertDpToPixels(10), (int)ConvertDpToPixels(10), (int)ConvertDpToPixels(10), (int)ConvertDpToPixels(10));
            ArrayAdapter<String> adapter = new ArrayAdapter<String>(context, Resource.Drawable.dropdown_item, items);
            dropdown.Adapter = adapter;
            
            if (values != null)
            {
                dropdown.SetSelection(adapter.GetPosition(
                    values.FirstOrDefault(x => x.Value == initialValue).Key
                ));

                dropdown.ItemSelected += (sender, args) =>
                {
                    string selected = (string)args.Parent?.GetItemAtPosition(args.Position)!;
                    write(values[selected]);
                };
            }

            return dropdown;
        }
        
    }
    
}