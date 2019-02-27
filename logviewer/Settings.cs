using logviewer.Interfaces;
using System.Configuration;

namespace logviewer.Properties
{


    // Diese Klasse ermöglicht die Behandlung bestimmter Ereignisse der Einstellungsklasse:
    //  Das SettingChanging-Ereignis wird ausgelöst, bevor der Wert einer Einstellung geändert wird.
    //  Das PropertyChanged-Ereignis wird ausgelöst, nachdem der Wert einer Einstellung geändert wurde.
    //  Das SettingsLoaded-Ereignis wird ausgelöst, nachdem die Einstellungswerte geladen wurden.
    //  Das SettingsSaving-Ereignis wird ausgelöst, bevor die Einstellungswerte gespeichert werden.
    public sealed partial class Settings : ISettings {
        
        public Settings() {
            this.SettingsLoaded += this.SettingsLoadedEventHandler;
            this.SettingChanging += this.SettingChangingEventHandler;
            this.SettingsSaving += this.SettingsSavingEventHandler;
        }

        private void SettingsLoadedEventHandler(object sender, SettingsLoadedEventArgs e)
        {
            //Query.DateTimeFormat = this.DateTimeFormat;
        }

        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // Fügen Sie hier Code zum Behandeln des SettingChangingEvent-Ereignisses hinzu.
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Fügen Sie hier Code zum Behandeln des SettingsSaving-Ereignisses hinzu.
        }
    }
}
