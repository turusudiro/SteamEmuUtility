using System;

namespace SteamCommon.Models
{
    public partial class SteamCallback
    {
        public event Action<bool> OnLoggedOn;
        public event Action<string> OnProgressUpdate;
        public event Action<App> OnAppCallback;

        public void InvokeLoggedOn(bool state)
        {
            OnLoggedOn?.Invoke(state);
        }

        public void InvokeProgressUpdate(string message)
        {
            OnProgressUpdate?.Invoke(message);
        }

        public void InvokeAppCallback(App data)
        {
            OnAppCallback?.Invoke(data);
        }
    }
}
