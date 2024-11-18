using GenericModConfigMenu;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using System;
using System.Linq;

namespace Sprinting
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; private set; } = null!; 
        internal static ModConfig Config { get; set; }
        internal static string GetFromi18n(string key) => Instance.Helper.Translation.Get(key);

        private PerScreen<int> _healthRegenTimer = new();
        private PerScreen<int> _energyRegenTimer = new();
        private PerScreen<int> _lastHealth = new();
        private PerScreen<float> _lastEnergy = new();
        private bool _staminaWarned = false;
        private bool _sprintOn = false;
        private string _buffId = "onemix.Sprinting_sprinting";

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.OnGameLaunched);
            helper.Events.GameLoop.OneSecondUpdateTicked += new EventHandler<OneSecondUpdateTickedEventArgs>(this.OnOneSecondUpdateTicked);
            helper.Events.GameLoop.UpdateTicked += new EventHandler<UpdateTickedEventArgs>(this.OnUpdateTicked);
            helper.Events.Input.ButtonsChanged += new EventHandler<ButtonsChangedEventArgs>(this.OnButtonsChanged);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
            {
                this.Monitor.Log("Generic Mod Config Menu not installed!", LogLevel.Debug);
                return;
            }
            else
            {
                this.Monitor.Log("Successfully hooked into spacechase0.GenericModConfigMenu.", LogLevel.Debug);
                configMenu.Register(
                    mod: this.ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => this.Helper.WriteConfig(Config)
                );
                configMenu.AddKeybindList(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.sprintingkey.name"),
                    getValue: () => Config.SprintingKey,
                    setValue: value => Config.SprintingKey = value
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.keyistoggle.name"),
                    tooltip: () => GetFromi18n("config.keyistoggle.tooltip"),
                    getValue: () => Config.KeyIsToggle,
                    setValue: value => Config.KeyIsToggle = value
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.cansprintexhausted.name"),
                    getValue: () => Config.CanSprintExhausted,
                    setValue: value => Config.CanSprintExhausted = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.speedbonus.name"),
                    getValue: () => Config.SpeedBoost,
                    setValue: value => Config.SpeedBoost = value,
                    min: 0,
                    max: 10
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.energydrainpersecond.name"),
                    tooltip: () => GetFromi18n("config.energydrainpersecond.tooltip"),
                    getValue: () => Config.EnergyDrainPerSecond,
                    setValue: value => Config.EnergyDrainPerSecond = value,
                    min: 0,
                    max: 10,
                    interval: 0.1f
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.skillbased.name"),
                    tooltip: () => GetFromi18n("config.skillbased.tooltip"),
                    getValue: () => Config.SkillBased,
                    setValue: value => Config.SkillBased = value
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.allowhorsesprinting.name"),
                    getValue: () => Config.AllowHorseSprinting,
                    setValue: value => Config.AllowHorseSprinting = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.minimumenergytosprint.name"),
                    tooltip: () => GetFromi18n("config.minimumenergytosprint.tooltip"),
                    getValue: () => Config.MinimumEnergyToSprint,
                    setValue: value => Config.MinimumEnergyToSprint = value
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.lowenergywarning.name"),
                    getValue: () => Config.LowEnergyWarning,
                    setValue: value => Config.LowEnergyWarning = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.energytowarn.name"),
                    getValue: () => Config.EnergyToWarn,
                    setValue: value => Config.EnergyToWarn = value
                );
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.canregenexhausted.name"),
                    getValue: () => Config.CanRegenExhausted,
                    setValue: value => Config.CanRegenExhausted = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.energyregenrate.name"),
                    tooltip: () => GetFromi18n("config.energyregenrate.tooltip"),
                    getValue: () => Config.EnergyRegenRate,
                    setValue: value => Config.EnergyRegenRate = value,
                    min: 0,
                    max: 10,
                    interval: 0.1f
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.maxenergyregen.name"),
                    tooltip: () => GetFromi18n("config.maxenergyregen.tooltip"),
                    getValue: () => Config.MaxEnergyRegen,
                    setValue: value => Config.MaxEnergyRegen = value,
                    min: 0,
                    max: 1f,
                    interval: 0.01f
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.energyregencooldown.name"),
                    tooltip: () => GetFromi18n("config.energyregencooldown.tooltip"),
                    getValue: () => Config.EnergyRegenCooldown,
                    setValue: value => Config.EnergyRegenCooldown = value
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.healthregenrate.name"),
                    tooltip: () => GetFromi18n("config.healthregenrate.tooltip"),
                    getValue: () => Config.HealthRegenRate,
                    setValue: value => Config.HealthRegenRate = value,
                    min: 0,
                    max: 10,
                    interval: 0.1f
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.maxhealthregen.name"),
                    tooltip: () => GetFromi18n("config.maxhealthregen.tooltip"),
                    getValue: () => Config.MaxHealthRegen,
                    setValue: value => Config.MaxHealthRegen = value,
                    min: 0,
                    max: 1f,
                    interval: 0.01f
                );
                configMenu.AddNumberOption(
                    mod: this.ModManifest,
                    name: () => GetFromi18n("config.healthregencooldown.name"),
                    tooltip: () => GetFromi18n("config.healthregencooldown.tooltip"),
                    getValue: () => Config.HealthRegenCooldown,
                    setValue: value => Config.HealthRegenCooldown = value
                );
            }
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;
            Farmer player = Game1.player;

            if (Config.SprintingKey.JustPressed() && Config.KeyIsToggle)
                _sprintOn = !_sprintOn;
            else if (Config.SprintingKey.IsDown())
                ApplySprintBuff(player);
        }

        public static bool CanSprint(Farmer player)
        {
            if (!Config.AllowHorseSprinting && player.isRidingHorse())
                return false;

            if (!Config.CanSprintExhausted && player.exhausted.Value)
                return false;

            if (!player.isMoving())
                return false;

            return player.Stamina >= Config.MinimumEnergyToSprint;
        }

        private Buff _sprintBuff;

        private void ApplySprintBuff(Farmer player)
        {
            if (CanSprint(player))
            {
                _sprintBuff ??= new Buff(
                    id: _buffId,
                    displayName: "Sprinting",
                    description: GetFromi18n("buff.sprinting.desc"),
                    iconTexture: this.Helper.ModContent.Load<Texture2D>("assets/sprinting.png"),
                    iconSheetIndex: 0,
                    duration: 100,
                    effects: new BuffEffects()
                    {
                        Speed = { Config.SpeedBoost }
                    }
                );

                Buff buff = Game1.buffsDisplay.GetSortedBuffs().Where(x => x.id == _buffId).FirstOrDefault();
                if (buff is not null)
                {
                    buff.millisecondsDuration = 100;
                }
                else
                {
                    _sprintBuff.millisecondsDuration = 100;
                    player.applyBuff(_sprintBuff);
                }
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;

            if (_sprintOn)
                ApplySprintBuff(player);

            bool canRegen = Context.IsMultiplayer ? Game1.currentLocation != null : Context.IsPlayerFree;

            bool canRegenExhausted = Config.CanRegenExhausted || !player.exhausted.Value;

            if (canRegen && canRegenExhausted)
            {
                float currentEnergyRatio = player.Stamina / player.MaxStamina;

                if (currentEnergyRatio < Config.MaxEnergyRegen && Config.EnergyRegenRate > 0f)
                {
                    if (player.Stamina < _lastEnergy.Value)
                        _energyRegenTimer.Value = Config.EnergyRegenCooldown;
                    else if (_energyRegenTimer.Value <= 0)
                        player.Stamina = Math.Min(player.MaxStamina, player.Stamina + player.MaxStamina * (Config.EnergyRegenRate / 100f / 60));
                }
            }

            _lastEnergy.Value = player.Stamina;

            if (!player.hasBuff(_buffId) || !player.isMoving() || !Context.IsPlayerFree || Game1.isFestival())
                return;

            float energyDrainPerSecond = Config.EnergyDrainPerSecond;

            if (Config.SkillBased && Config.EnergyDrainPerSecond > 0f)
            {
                int skillAmount = 5;
                int totalLevels = 0;
                int maxLevelPerSkill = 10;
                for (int i = 0; i < skillAmount; i++)
                    totalLevels += player.GetSkillLevel(i);
                energyDrainPerSecond += Math.Max(skillAmount - totalLevels / maxLevelPerSkill, 0);
            }

            player.Stamina -= energyDrainPerSecond / 60;
        }

        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            Farmer player = Game1.player;

            if (player.Stamina <= Config.EnergyToWarn && Config.SprintingKey.IsDown() && !_staminaWarned && Config.LowEnergyWarning)
            {
                Game1.addHUDMessage(new HUDMessage(GetFromi18n("message.lowenergy"), HUDMessage.error_type));
                _staminaWarned = true;
            }
            else if (player.Stamina >= Config.EnergyToWarn * 1.4 && _staminaWarned && Config.LowEnergyWarning)
            {
                _staminaWarned = false;
            }

            bool canRegen = Context.IsMultiplayer? Game1.currentLocation != null : Context.IsPlayerFree;

            if (canRegen)
            {
                float currentHealthRatio = player.health / player.maxHealth;

                if (currentHealthRatio < Config.MaxHealthRegen && Config.HealthRegenRate > 0f)
                {
                    if (player.health < _lastHealth.Value)
                        _healthRegenTimer.Value = Config.HealthRegenCooldown;
                    else if (_healthRegenTimer.Value > 0)
                        --_healthRegenTimer.Value;
                    else
                        player.health = Math.Min(player.maxHealth, player.health + (int)(player.maxHealth * (Config.HealthRegenRate / 100f)));
                }

                _lastHealth.Value = player.health;

                if (_energyRegenTimer.Value > 0)
                    --_energyRegenTimer.Value;
            }
        }
    }
}
