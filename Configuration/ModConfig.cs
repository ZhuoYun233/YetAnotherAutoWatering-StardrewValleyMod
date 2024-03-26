using StardewModdingAPI;

namespace YetAnotherAutoWatering.Configuration
{
    /// <summary>
    /// Config file for Autowatering
    /// </summary>
    public class ModConfig
    {
        /// <summary>
        /// Sets whether this mod is enabled, default true
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Sets the button to reload this config file, default f5
        /// </summary>
        public SButton ConfigReloadKey { get; set; } = SButton.F5;

        /// <summary>
        /// Optional integer that represents the fertilizer to auto apply to all, default null
        /// Possible values:
        ///     null: Disable auto changing the fertilizer
        ///     0: Always remove all fertilizer
        ///     368: Basic Fertilizer
        ///     369: Quality Fertilizer
        ///     370: Basic Retaining Soil
        ///     371: Quality Retaining Soil
        ///     465: Speed-Gro
        ///     466: Deluxe Speed-Gro
        ///     918: Hyper Speed-Gro
        ///     919: Deluxe Fertilizer
        ///     920: Deluxe Retaining Soil
        ///     
        /// </summary>
        public int? Fertilizer { get; set; } = null;

        /// <summary>
        /// Sets what days of the week to auto water, default every day
        /// </summary>
        public DaysToWater DaysToWater { get; set; } = new DaysToWater();
    }
}
