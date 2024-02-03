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
using Android.Graphics.Drawables;
using Android.Net;
using Android.Provider;
using Java.IO;
using Fragment = AndroidX.Fragment.App.Fragment;
using MWP.BackEnd;
using MWP.Helpers;
using Color = Android.Graphics.Color;
using Orientation = Android.Widget.Orientation;
using Uri = Android.Net.Uri;

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
        private List<string> paths;
        private AlertDialog? dialog;


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
            TextView? settingsTestbtbn = view?.FindViewById<TextView>(Resource.Id.settings_testbtn);
            settingsTestbtbn.Click += delegate
            {
                List<string> dd = new List<string>() { "jeden song", "druhhy song", "ttreti song" };
                if (assets != null) UiRenderFunctions.ListIncomingSongsPopup(dd, "lukas", 15, context, () => { }, () => { });
            }; */

            /*
             * Príjmanie dáta nastavení z pozadia aplikácie. Rozlišijeme 3 typi nastavení
             * Bool - Nastavenie reprezentované komponentom Switch
             * Int  - Nastavenie reprezentované komponentom Spinner typu dropdown
             * Folder Picker - statické nastavenie zadefinované v XML. Môže sa nachádzať iba raz.
             */
            List<(string name, Func<bool> read, Action<bool> write, string? remark)> boolSettings = SettingsManager.GetBoolSettings();
            List<(string name, Func<int> read, Action<int> write, Dictionary<string, int>? mapping, string? remark)> intSettings = SettingsManager.GetIntSettings();

            /*
             * Vykreslovanie všetkých n-počet Bool nastavení získaných z pozadia. Vytváram for loop podľa počtu
             * nastavení v liste. Pre každý element v liste nastavení volám funkciy DropdownSetting ktorá vráti
             * LinearLayout obsahujúci užívateľské rozhranie pripravené na vykreslenie.
             */
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
            
            /*
             * Vykreslovanie všetkých n-počet Int nastavení získaných z pozadia. Vytváram for loop podľa počtu
             * nastavení v liste. Pre každý element v liste nastavení volám funkciy DropdownSetting ktorá vráti
             * LinearLayout obsahujúci užívateľské rozhranie pripravené na vykreslenie.
             */
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
            
            
            /*
             * Restricted MP3 folders setting
             */
            TextView? restrictedTitle = view?.FindViewById<TextView>(Resource.Id.settings_restricted_title);
            if (restrictedTitle != null) restrictedTitle.Typeface = font;
            TextView? restrictedRemark = view?.FindViewById<TextView>(Resource.Id.settings_restricted_remark);
            if (restrictedRemark != null) restrictedRemark.Typeface = font;
            TextView? addNewRestricted = view?.FindViewById<TextView>(Resource.Id.settings_restricted_button);
            if (addNewRestricted != null)
            {
                addNewRestricted.Typeface = font;
                // List<string> paths = new List<string>{ "path1", "path2", "path3", "path4" };
                paths = SettingsManager.ExcludedPaths;
                addNewRestricted.Click += delegate { ListPathsPopup(paths); };
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
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context)
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
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(20.0f, context)
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
        
        
        /**/
        private AndroidX.AppCompat.Widget.SwitchCompat CreateSwitch(Action<bool> write, bool initialValue)
        {
            AndroidX.AppCompat.Widget.SwitchCompat switchCompat = new AndroidX.AppCompat.Widget.SwitchCompat(context);
            LinearLayout.LayoutParams switchParams = new LinearLayout.LayoutParams(
                (int)UiRenderFunctions.ConvertDpToPixels(60.0f, context),
                (int)UiRenderFunctions.ConvertDpToPixels(50.0f, context)
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
        
        /*
         * Meoda slúžiaca na vytvorenie nového Spinner komponentu typu dropdown pre nastavenie typu Int,
         * v ktorom každý riadok predstavuje jednu z monžostí pre používateľa daného nastavenia.
         * Dáta pre rolovací list sú získavané z pozadia aplikácie
         * ktoré sa následne musia vložiť ako argumentu ktoré príjma táto metóda.
         */
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
            dropdown.SetPadding(
                (int)UiRenderFunctions.ConvertDpToPixels(10, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(10, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(10, context), 
                (int)UiRenderFunctions.ConvertDpToPixels(10, context)
            );
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
        
        /*
         * Privátna metóda slúžiaca na vytvorenie AlertDialogu v ktorom sa nachádza ScrollView s listom
         * zakázaných ciest priečinkov pre hľadanie MP3 súborov a tlačidlom pre možnosť pridania novej
         * cesty prostredníctvom Folder Picker-u. 
         */
        private void ListPathsPopup(List<string> paths)
        {
            LayoutInflater? ifl = LayoutInflater.From(context);
            View? view = ifl?.Inflate(Resource.Layout.settings_restricted_paths_popup, null);
            AlertDialog.Builder alert = new AlertDialog.Builder(context);
            alert.SetView(view);

            dialog = alert.Create();
            dialog?.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));

            LinearLayout? ln = view?.FindViewById<LinearLayout>(Resource.Id.settings_restricted_path_list);
            
            /*
             * Prechádzanie cestami v liste a pre každú cestu vytvorenie nového záznamu v liste v ScrollView.
             */
            foreach (string path in paths)
            {
                /*
                 * Vytvaranie LinearLayout-u ktory bude obsahpvat text cesty a tlacidlo na vymazanie
                 */
                LinearLayout lnIn = new LinearLayout(context);
                lnIn.Orientation = Orientation.Horizontal;
                lnIn.SetBackgroundResource(Resource.Drawable.rounded_light);

                LinearLayout.LayoutParams lnInParams = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent
                );
                lnInParams.SetMargins(20, 20, 20, 20);
                lnIn.LayoutParameters = lnInParams;
                lnIn.SetPadding(0, (int)UiRenderFunctions.ConvertDpToPixels(10, context), 0, (int)UiRenderFunctions.ConvertDpToPixels(10, context));
                lnIn.SetGravity(GravityFlags.Center);

                /*
                 * Vytváranie TextView komponentu pre názov cesty
                 */
                LinearLayout.LayoutParams nameParams = new LinearLayout.LayoutParams(
                    0,
                    ViewGroup.LayoutParams.WrapContent
                );
                nameParams.Weight = 1;
                TextView name = new TextView(context);
                name.SetPadding(
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context)

                );
                name.LayoutParameters = nameParams;
                name.TextSize = (int)UiRenderFunctions.ConvertDpToPixels(5, context);
                name.Typeface = font;
                name.SetTextColor(Color.White);
                int index = path.IndexOf(FileManager.Root, StringComparison.Ordinal);
                name.Text = (index < 0)
                    ? path
                    : path.Remove(index, FileManager.Root.Length);
                lnIn.AddView(name);
                
                /*
                 * Vytváranie tlačidla slúžiaceho na vymazanie aktuálneho riadka z listu ciest
                 */
                LinearLayout.LayoutParams deleteButtonParams = new LinearLayout.LayoutParams(
                    (int)UiRenderFunctions.ConvertDpToPixels(30, context),
                    ViewGroup.LayoutParams.WrapContent
                );
                deleteButtonParams.SetMargins(0, 0, (int)UiRenderFunctions.ConvertDpToPixels(10, context), 0);
                TextView deleteButton = new TextView(context);
                deleteButton.LayoutParameters = deleteButtonParams;
                deleteButton.TextSize = (int)UiRenderFunctions.ConvertDpToPixels(3, context);
                deleteButton.Typeface = font;
                deleteButton.TextAlignment = TextAlignment.Center;
                deleteButton.SetForegroundGravity(GravityFlags.Center);
                deleteButton.SetPadding(
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context),
                    (int)UiRenderFunctions.ConvertDpToPixels(10, context)

                );
                deleteButton.SetTextColor(Color.White);
                deleteButton.SetBackgroundResource(Resource.Drawable.rounded_button);
                deleteButton.Text = "X";
                deleteButton.Click += delegate
                {
                    paths.Remove(path);
                    SettingsManager.ExcludedPaths = paths;
                    dialog?.Cancel();
                    ListPathsPopup(paths);
                };
                lnIn.AddView(deleteButton);
                
                ln?.AddView(lnIn);
            }
            
            
            TextView? addNew = view?.FindViewById<TextView>(Resource.Id.settings_restricted_add_new_restricted);
            if (addNew != null)
            {
                addNew.Typeface = font;
                addNew.Click += (_, _) =>
                {
                    if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop){ 
                        Intent i = new Intent(Intent.ActionOpenDocumentTree); 
                        i.AddCategory(Intent.CategoryDefault);
                        StartActivityForResult(Intent.CreateChooser(i, "Choose directory") ?? new Intent(), 9999);
                    }
                };
            }

            TextView? cancel = view?.FindViewById<TextView>(Resource.Id.settings_restricted_cancel);
            if (cancel != null)
            {
                cancel.Typeface = font;
                cancel.Click += (_, _) => { dialog?.Hide(); };
            }
            
            dialog?.Show();
        }
        
        public override void OnActivityResult(int requestCode, int resultCode, Intent data) {
            base.OnActivityResult(requestCode, resultCode, data);

            switch(requestCode) {
                case 9999:
                    if (data.Data?.Path != null)
                    {
                        File file = new File(data.Data.Path);//create path from uri
                        String[] split = file.Path.Split(System.IO.Path.PathSeparator);//split the path.
                        string filePath = $"{FileManager.Root}{System.IO.Path.DirectorySeparatorChar}{split[1]}";//assign it to a string(your choice).
                        paths.Add(filePath);
                        SettingsManager.ExcludedPaths = paths;
#if DEBUG
                        MyConsole.WriteLine($"Result URI {filePath}");
#endif
                        dialog?.Cancel();
                        ListPathsPopup(paths);
                    }

                    break;
            }
        }
        
    }
    
}