using AutoWateringNew.Configuration;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoWateringNew
{
    /// <summary>The mod entry point.</summary>
    internal class ModEntry : Mod
    {
        //list of hoeDirt to water
        private readonly HashSet<HoeDirt> _hoeDirts = new HashSet<HoeDirt>();
        //list of indoor pots to water
        private readonly HashSet<IndoorPot> _indoorPots = new HashSet<IndoorPot>();
        
        private ModConfig _config;
        private bool _shouldWaterToday = true;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.World.TerrainFeatureListChanged += this.OnTerrainFeatureListChanged;
            helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == _config.ConfigReloadKey)
            {
                _config = Helper.ReadConfig<ModConfig>();
                Monitor.Log("Config file reloaded", LogLevel.Warn);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            _shouldWaterToday = ShouldAutoWaterToday();

            var builtLocations = Game1.locations.OfType<GameLocation>()
                .SelectMany(location => location.buildings)
                .Select(building => building.indoors.Value)
                .Where(location => location != null);
            var allLocations = Game1.locations.Concat(builtLocations);

            foreach (var location in allLocations)
            {
                foreach (var indoorPot in location.objects.Values.OfType<IndoorPot>())
                    SetupIndoorPotListeners(indoorPot);

                foreach (var hoeDirt in location.terrainFeatures.Values.OfType<HoeDirt>())
                    SetupHoeDirtListeners(hoeDirt);
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            _shouldWaterToday = ShouldAutoWaterToday();

            if (!_shouldWaterToday)
                return;

            foreach (var hoeDirt in _hoeDirts)
                this.Water(hoeDirt);
            foreach (var indoorPot in _indoorPots)
                this.Water(indoorPot.hoeDirt.Value, indoorPot);
        }
        /// <summary>
        /// Check if the recent changed object is indoor pot. If so, setup indoor pot listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            foreach (var kvp in e.Added)
            {
                var obj = kvp.Value;
                if (obj is IndoorPot indoorPot)
                    SetupIndoorPotListeners(indoorPot);
            }
        }
        /// <summary>
        /// Check if the recent changed terrain feature is hoeDirt. If so, setup hoeDirt listener.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTerrainFeatureListChanged(object sender, TerrainFeatureListChangedEventArgs e)
        {
            foreach (var kvp in e.Added)
            {
                var feature = kvp.Value;
                if (feature is HoeDirt hoeDirt)
                    SetupHoeDirtListeners(hoeDirt);
            }
        }

        private void SetupIndoorPotListeners(IndoorPot indoorPot)
        {
            SetupHoeDirtListeners(indoorPot.hoeDirt.Value, indoorPot);
        }

        private void SetupHoeDirtListeners(HoeDirt hoeDirt, IndoorPot pot = null)
        {
            try
            {
                var netCrop = Helper.Reflection.GetField<NetRef<Crop>>(hoeDirt, "netCrop", true).GetValue();
                //Check if the hoeDirt have crop grown, if so, water
                if (netCrop.Value != null)
                    TrackAndWater(hoeDirt, pot);
                //Add new method with new parameter 'crop' to auto TrackAndWater or RemoveTrack when the specific field object changes
                netCrop.fieldChangeVisibleEvent += (_, __, crop) =>
                {
                    if (crop != null)
                        TrackAndWater(hoeDirt, pot);
                    else
                        RemoveTrack(hoeDirt, pot);
                };
            }
            catch (Exception ex)
            {
                Monitor.Log($"Could not read crop data on dirt; possible new game version?\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Track if the hoe dirt is in pot or not, add it to the list correspondingly, and water all items in both lists.
        /// </summary>
        /// <param name="hoeDirt"></param>
        /// <param name="indoorPot"></param>
        private void TrackAndWater(HoeDirt hoeDirt, IndoorPot indoorPot = null)
        {
            if (indoorPot != null)
                _indoorPots.Add(indoorPot);
            else
                _hoeDirts.Add(hoeDirt);
            Water(hoeDirt, indoorPot);
        }

        private void RemoveTrack(HoeDirt hoeDirt, IndoorPot indoorPot = null)
        {
            if (indoorPot != null)
                _indoorPots.Remove(indoorPot);
            else
                _hoeDirts.Remove(hoeDirt);
        }

        private void Water(HoeDirt hoeDirt, IndoorPot pot = null)
        {
            if (!_config.Enabled || !_shouldWaterToday)
                return;

            hoeDirt.state.Value = HoeDirt.watered;
            if (_config.Fertilizer != null)
                ;
               // hoeDirt.fertilizer.Value = _config.Fertilizer.Value;
            if (pot != null)
                pot.showNextIndex.Value = true;
        }

        private bool ShouldAutoWaterToday()
        {
            // Stardew dates range from 1->28
            // The date mod 7 gives us the current day of the week
            switch (Game1.dayOfMonth % 7)
            {
                case 0: return _config.DaysToWater.Sunday;
                case 1: return _config.DaysToWater.Monday;
                case 2: return _config.DaysToWater.Tuesday;
                case 3: return _config.DaysToWater.Wednesday;
                case 4: return _config.DaysToWater.Thursday;
                case 5: return _config.DaysToWater.Friday;
                case 6: return _config.DaysToWater.Saturday;
                default: return true;
            }
        }
    }
}
