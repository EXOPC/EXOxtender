using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXOxtenderLibrary
{
    public class VolumeChangedEventArgs : EventArgs
    {
        public VolumeChangedEventArgs(int volumeLevel)
        {
            newVol = volumeLevel;
        }
        private int newVol;
        public int Volume
        {
            get { return newVol; }
        }
    }

    public class VolumeControl
    {
        private MMDevice device;
        MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();

        public delegate void VolumeEventHandler(object sender, VolumeChangedEventArgs e);
        public event VolumeEventHandler VolumeChanged;

        //Private constructor
        VolumeControl() 
        {
            device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            //device.AudioEndpointVolume.OnVolumeNotification -= new AudioEndpointVolumeNotificationDelegate(Volume_OnVolumeNotification);
            device.AudioEndpointVolume.OnVolumeNotification += new AudioEndpointVolumeNotificationDelegate(Volume_OnVolumeNotification);
        }

        //Nested class for lazy instantiation
        class VolumeControlCreator
        {
            static VolumeControlCreator() { }

            //Private object instantiated with private constructor
            internal static readonly VolumeControl uniqueInstance = new VolumeControl();
        }

        //Public static property to get the object
        public static VolumeControl Instance
        {
            get { return VolumeControlCreator.uniqueInstance; }
        }

        public int GetVolume()
        {
            //device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }

        public int isMute
        {
            get
            {
                int isMute = 0;
                switch (this.Mute)
                {
                    case true:
                        isMute = 1;
                        break;
                }
                return isMute;
            }
        }

        public bool Mute
        {
            get { //device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia); 
                return device.AudioEndpointVolume.Mute; }
            set { //device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia); 
                device.AudioEndpointVolume.Mute = value; }
        }

        public void SetVolume(int newVolume)
        {            
            if ((device == null))
            {
                device = DevEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            }
            device.AudioEndpointVolume.MasterVolumeLevelScalar = ((float)newVolume / 100.0f);
            //return GetVolume();
        }

        void Volume_OnVolumeNotification(AudioVolumeNotificationData data)
        {
            if (VolumeChanged != null)
                VolumeChanged(this, new VolumeChangedEventArgs((int)(data.MasterVolume * 100)));
        }
    }

}