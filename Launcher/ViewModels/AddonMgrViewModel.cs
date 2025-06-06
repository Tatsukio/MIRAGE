using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MIRAGE_Launcher.Models;

namespace MIRAGE_Launcher.ViewModels
{
    public class AddonMgrViewModel
    {
        public static ObservableCollection<Addon> Addons { get; set; } = [];

        public AddonMgrViewModel()
        {
            Load();
        }

        public static void Load()
        {
            Addons = AddonMgr.ToObservableCollection(AddonMgr.Addons);
            foreach (var addon in Addons)
            {
                addon.PropertyChanged += OnAddonChange;
            }

            Addons.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (Addon addon in e.NewItems)
                    {
                        addon.PropertyChanged += OnAddonChange;
                    }
                }

                if (e.OldItems != null)
                {
                    foreach (Addon addon in e.OldItems)
                    {
                        addon.PropertyChanged -= OnAddonChange;
                    }
                }
            };
        }

        private static void OnAddonChange(object sender, PropertyChangedEventArgs e)
        {
            AddonMgr.Addons = AddonMgr.ToList(Addons);
            Settings.Set("AddonMgr/EnabledMods", string.Join(',', AddonMgr.GetEnabledAddons(AddonMgr.Addons).Select(a => a.Id)));
        }
    }
}
