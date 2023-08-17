namespace Loupedeck.StreamlabsPlugin
{
    using System;

    internal class AudioSourceDescriptor
    {
        public Boolean SpecialSource;
        public Boolean Muted;
        public Single Volume;

        public AudioSourceDescriptor(String name, Object that, Boolean isSpecSource = false)
        {
            this.Muted = false;
            this.Volume = 0;
            this.SpecialSource = isSpecSource;

            try
            {   /*NB. All volume in decibels!*/
                var v = 0.0F;                //that.GetVolume(name,true);
                this.Muted = false; // v.Muted;
                this.Volume = v; //v.Volume;
            }
            catch (Exception ex)
            {
                Tracer.Error($"Exception {ex.Message} getting volume information for source {name}");
            }
        }
    }


}
