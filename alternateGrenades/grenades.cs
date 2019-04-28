using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using InfinityScript;
using static InfinityScript.GSCFunctions;

/// <summary>
/// Custom grenade behaviors
/// 
/// Frag - Unmodified?/Flame Frag
/// Semtex - Impact Grenade
/// Throwing Knife - 
/// Bouncing Betty - Shockwave Grenade
/// Claymore - Trip Mine?
/// C4 - IED
/// 
/// Concussion Grenade - Knockback Grenade (Toggle knock weapon out of hand)
/// Flash Grenade - Nine Bang
/// Smoke Grenade - Nova Gas?
/// EMP - QED
/// Scrambler - Radio Player/Noise Generator (full blast)
/// Portable Radar - Solar Reflector?
/// Trophy System - Radiation Generator
/// Tactical Insertion - Glowstick
/// </summary>

namespace alternateGrenades
{
    public class grenades : BaseScript
    {
        public static int fx_glowstickGlow_red;
        public static int fx_glowstickGlow_green;
        private static int fx_flashbang;
        private static int fx_fire;
        private static int fx_radiation;
        private static int fx_sentryExplode;
        private static int fx_sentrySmoke;
        private static int fx_reflectorSparks;
        private static int fx_radioLight;
        private static int fx_mineLaunch;
        private static int fx_mineSpin;
        private static int fx_mineExplode;
        private static int fx_empExplode;
        private static int fx_tracer_single;
        private static int fx_tracer_shotgun;
        private static int fx_disappear;
        private static int fx_laser;
        private static string fx_flare;
        public static bool isTeamBased = true;
        public static Dictionary<string, string> otherTeam = new Dictionary<string, string>();
        private static List<string> radioNoises = new List<string>();
        private static List<Entity> glowsticks = new List<Entity>();

        public grenades()
        {
            Utilities.PrintToConsole("Flare is " + GetDvar("r_sunflare_shader"));
            fx_flare = GetDvar("r_sunflare_shader");
            PreCacheItem("lightstick_mp");
            PreCacheShellShock("dcburning");
            PreCacheShellShock("radiation_low");
            PreCacheShellShock("radiation_med");
            PreCacheShellShock("radiation_high");
            PreCacheShellShock("dog_bite");
            Utilities.PrintToConsole("Precaching sunflare " + fx_flare);
            PreCacheShader(fx_flare);
            fx_glowstickGlow_green = LoadFX("misc/glow_stick_glow_green");
            fx_glowstickGlow_red = LoadFX("misc/glow_stick_glow_red");
            fx_flashbang = LoadFX("explosions/flashbang");
            fx_fire = LoadFX("fire/tank_fire_engine");
            fx_radiation = LoadFX("distortion/distortion_tank_muzzleflash");
            fx_sentryExplode = LoadFX("explosions/sentry_gun_explosion");
            fx_sentrySmoke = LoadFX("smoke/car_damage_blacksmoke");
            fx_reflectorSparks = LoadFX("explosions/sparks_a");
            fx_radioLight = LoadFX("misc/aircraft_light_cockpit_red");
            fx_mineLaunch = LoadFX("impacts/bouncing_betty_launch_dirt");
            fx_mineSpin = LoadFX("dust/bouncing_betty_swirl");
            fx_mineExplode = LoadFX("explosions/bouncing_betty_explosion");
            fx_empExplode = LoadFX("explosions/emp_grenade");
            fx_tracer_single = LoadFX("impacts/exit_tracer");
            fx_tracer_shotgun = LoadFX("impacts/shotgun_default");
            fx_disappear = LoadFX("impacts/small_snowhit");
            fx_laser = LoadFX("misc/claymore_laser");

            otherTeam["allies"] = "axis";
            otherTeam["axis"] = "allies";
            otherTeam["none"] = "none";

            radioNoises.Add("sentry_drop");
            radioNoises.Add("sentry_gun_hydraulics");
            radioNoises.Add("ac130_105mm_reload");
            radioNoises.Add("ac130_40mm_reload");
            radioNoises.Add("cobra_helicopter_crash_dist");
            radioNoises.Add("distant_artillery_barrage");
            radioNoises.Add("exp_ac130_105mm_dist");
            radioNoises.Add("exp_ac130_105mm_dist_sub");
            radioNoises.Add("exp_ac130_40mm_dist");
            radioNoises.Add("fast_artillery_round");
            radioNoises.Add("foly_onemanarmy_bag6_npc");
            radioNoises.Add("ims_rocket_fire_npc");
            radioNoises.Add("talon_damaged");
            radioNoises.Add("talon_rocket_reload_plr");
            radioNoises.Add("freefall_death");
            radioNoises.Add("scrambler_beep");
            radioNoises.Add("generic_death_american_1");
            radioNoises.Add("generic_death_russian_1");
            radioNoises.Add("weap_aa12silenced_fire_npc");
            radioNoises.Add("vending_machine_soda_drop");
            radioNoises.Add("plant_large_break");
            radioNoises.Add("plant_large_break_debris");
            radioNoises.Add("dst_waterbottle_hit");
            radioNoises.Add("dst_LCD_flatpannel");
            radioNoises.Add("elm_wolf");
            radioNoises.Add("elm_metal_rattle");
            radioNoises.Add("physics_tv_default");
            radioNoises.Add("trophy_explode_layer");
            radioNoises.Add("semtex_warning");
            radioNoises.Add("rocket_explode_sand_layer");
            radioNoises.Add("movement_foliage");
            radioNoises.Add("explo_tree");

            SetDevDvarIfUninitialized("g_stunDropsWeapon", 0);

            string gametype = GetDvar("g_gametype");
            if (gametype == "dm" || gametype == "gun" || gametype == "oic" || gametype == "jugg")
                isTeamBased = false;

            PlayerConnected += OnPlayerConnected;
            //Notified += onGlobalNotify;

            Marshal.WriteInt32(new IntPtr(0x147D5A04), 1);//Throwingknife damage
        }
        public static void OnPlayerConnected(Entity player)
        {
            player.SetField("glowstickOut", false);
            player.SpawnedPlayer += () => OnPlayerSpawned(player);
            player.OnNotify("grenade_fire", (p, g, w) => onGrenadeFire(p, (Entity)g, (string)w));
            player.OnNotify("trigger_use", (p) => onPlayerUseTrigger(p));
            player.NotifyOnPlayerCommand("trigger_use", "+activate");

            //Hit feedback
            HudElem hitFeedback = NewClientHudElem(player);
            hitFeedback.HorzAlign = HudElem.HorzAlignments.Center;
            hitFeedback.VertAlign = HudElem.VertAlignments.Middle;
            hitFeedback.X = -12;
            hitFeedback.Y = -12;
            hitFeedback.Archived = false;
            hitFeedback.HideWhenDead = false;
            hitFeedback.Sort = 0;
            hitFeedback.Alpha = 0;
            player.SetField("hud_hitFeedback", hitFeedback);
        }
        public static void OnPlayerSpawned(Entity player)
        {
            spawnPlayerAtGlowstick(player);
            checkForGlowstick(player);
        }

        public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            if (weapon == "frag_grenade_mp")
            {
                if (!player.IsAlive) return;
                if (mod != "MOD_GRENADE_SPLASH") return;

                OnInterval(750, () => runFragFire(player, attacker));
            }
            else if (weapon == "concussion_grenade_mp")
            {
                if (!player.IsAlive) return;
                if (mod != "MOD_GRENADE_SPLASH") return;

                Vector3 angle = VectorToAngles(player.Origin - inflictor.Origin);
                angle = AnglesToForward(angle);
                angle.Normalize();
                player.SetVelocity(new Vector3(angle.X * 100, angle.Y * 100, 100));
                OnInterval(50, () => waitForPlayerGrounded(player));
                if (GetDvarInt("g_stunDropsWeapon") > 0)
                {
                    string currentWeapon = player.CurrentWeapon;
                    player.DropItem(currentWeapon);
                }
            }
            else if (weapon == "throwingknife_mp")
            {
                Utilities.PrintToConsole("Hit by knife");
                player.Health = 1;
                player.SetCanDamage(false);
                player.SetCanRadiusDamage(false);

                AfterDelay(0, () => checkForKnifeGrab(player, attacker, inflictor));
            }
        }
        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (player.HasField("trophy"))
                StartAsync(trophyBreak(player.GetField<Entity>("trophy")));
            if (player.HasField("reflector"))
                StartAsync(reflectorBreak(player.GetField<Entity>("reflector")));
            if (player.HasField("radio"))
                StartAsync(radioBreak(player.GetField<Entity>("radio")));
            if (player.HasField("shockwave"))
                deleteShockwave(player.GetField<Entity>("shockwave"));
        }
        public override void OnPlayerDisconnect(Entity player)
        {
            if (player.HasField("trophy"))
                StartAsync(trophyBreak(player.GetField<Entity>("trophy")));
            if (player.HasField("reflector"))
                StartAsync(reflectorBreak(player.GetField<Entity>("reflector")));
            if (player.HasField("radio"))
                StartAsync(radioBreak(player.GetField<Entity>("radio")));
            if (player.HasField("shockwave"))
                deleteShockwave(player.GetField<Entity>("shockwave"));
            if (player.HasField("glowstick"))
                deleteGlowstick(player.GetField<Entity>("glowstick"));
        }

        private static void onPlayerUseTrigger(Entity player)
        {
            if (player.HasField("trophy") && player.Origin.DistanceTo(player.GetField<Entity>("trophy").GetField<Entity>("trigger").Origin) < 125)
                pickupTrophy(player, player.GetField<Entity>("trophy"));
            else if (player.HasField("reflector") && player.Origin.DistanceTo(player.GetField<Entity>("reflector").GetField<Entity>("trigger").Origin) < 125)
                pickupReflector(player, player.GetField<Entity>("reflector"));
            else if (player.HasField("radio") && player.Origin.DistanceTo(player.GetField<Entity>("radio").GetField<Entity>("trigger").Origin) < 125)
                pickupRadio(player, player.GetField<Entity>("radio"));
            else if (player.HasField("shockwave") && player.Origin.DistanceTo(player.GetField<Entity>("shockwave").GetField<Entity>("trigger").Origin) < 125)
                pickupShockwave(player, player.GetField<Entity>("shockwave"));
            else if (player.HasField("glowstick") && player.Origin.DistanceTo(player.GetField<Entity>("glowstick").GetField<Entity>("trigger_friendly").Origin) < 125)
                pickupGlowstick(player, player.GetField<Entity>("glowstick"));
            else if (glowsticks.Count > 0)
            {
                foreach (Entity glowstick in glowsticks)
                {
                    if (player.Origin.DistanceTo(glowstick.GetField<Entity>("trigger").Origin) < 125)
                    {
                        if ((isTeamBased && glowstick.GetField<Entity>("owner").SessionTeam != player.SessionTeam) || !isTeamBased)
                            deleteGlowstick(glowstick, player);
                    }
                }
            }
        }

        public static void updateDamageFeedback(Entity player, string type)
        {
            HudElem hitFeedback = player.GetField<HudElem>("hud_hitFeedback");

            string shader = "damage_feedback";
            if (type == "deployable_vest")
                shader = "damage_feedback_lightarmor";
            else if (type == "juggernaut")
                shader = "damage_feedback_juggernaut";

            hitFeedback.SetShader(shader, 24, 48);
            hitFeedback.Alpha = 1;
            //player.SetField("hud_damageFeedback", hitFeedback);
            player.PlayLocalSound("player_feedback_hit_alert");

            hitFeedback.FadeOverTime(1);
            hitFeedback.Alpha = 0;
            //AfterDelay(1000, () => hitFeedback.Destroy());
        }

        private static void onGrenadeFire(Entity player, Entity grenade, string weaponName)
        {
            if (!player.IsAlive) return;

            if (weaponName == "lightstick_mp")//glowstick
            {
                if (player.HasField("glowstick"))
                    deleteGlowstick(player.GetField<Entity>("glowstick"));

                //StartAsync(waitForGlowstickDrop(player, grenade));
                dropGlowstick(player, player.Origin);
            }
            else if (weaponName == "semtex_mp")//semtex
                StartAsync(waitForSemtexStuck(grenade));
            else if (weaponName == "c4_mp")//ied
            {
                grenade.SetField("owner", player);
                StartAsync(waitForC4Stuck(grenade));
            }
            else if (weaponName == "flash_grenade_mp")
                StartAsync(waitForFlashExplode(player, grenade));
            //else if (weaponName == "frag_grenade_mp")//Handled in onPlayerDamage
            else if (weaponName == "trophy_mp")
            {
                grenade.Delete();
                if (player.IsOnGround())//TODO: check ground distance similar to GSC
                {
                    if (player.HasField("trophy"))
                        deleteTrophy(player.GetField<Entity>("trophy"));
                    Entity trophy = createTrophy(player);
                    player.SetField("trophy", trophy);
                    createTrophyRadiator(player, trophy);
                }
                else
                    player.SetWeaponAmmoStock("trophy_mp", player.GetWeaponAmmoStock("trophy_mp") + 1);
            }
            else if (weaponName == "portable_radar_mp")
            {
                if (player.HasField("reflector"))
                    deleteReflector(player.GetField<Entity>("reflector"));
                grenade.SetField("owner", player);
                StartAsync(waitForRadarStuck(grenade));
            }
            else if (weaponName == "scrambler_mp")
            {
                if (player.HasField("radio"))
                    deleteRadio(player.GetField<Entity>("radio"));
                grenade.Delete();
                if (player.IsOnGround())//TODO: check ground distance similar to GSC
                {
                    Entity radio = createRadioPlayer(player);
                    player.SetField("radio", radio);
                }
                else
                    player.SetWeaponAmmoStock("scrambler_mp", player.GetWeaponAmmoStock("scrambler_mp") + 1);
            }
            else if (weaponName == "smoke_grenade_mp")
                StartAsync(waitForSmokeExplode(player, grenade));
            else if (weaponName == "bouncingbetty_mp")
            {
                if (player.HasField("shockwave"))
                    deleteShockwave(player.GetField<Entity>("shockwave"));
                grenade.SetField("owner", player);
                StartAsync(waitForBettyStuck(grenade));
            }
            else if (weaponName == "emp_grenade_mp")
                StartAsync(primeQED(player, grenade));
            else if (weaponName == "claymore_mp")
            {
            }
        }

        #region IED
        private static IEnumerator waitForC4Stuck(Entity grenade)
        {
            yield return grenade.WaitTill("activated");

            StartAsync(watchForIEDEnd(grenade));
            OnInterval(50, () => watchIED(grenade));
        }
        private static bool watchIED(Entity grenade)
        {
            if (grenade.GetField<Entity>("owner").Classname != "player" || !grenade.GetField<Entity>("owner").IsAlive) return false;
            if (grenade.HasField("cancel"))
            {
                grenade.ClearField("cancel");
                return false;
            }

            foreach (Entity players in Players)
            {
                if (players.Classname != "player" || !players.IsAlive) continue;
                if (isTeamBased && players.SessionTeam == grenade.GetField<Entity>("owner").SessionTeam) continue;
                else if (!isTeamBased && players == grenade.GetField<Entity>("owner")) continue;

                if (players.Origin.DistanceTo(grenade.Origin) < 128)
                {
                    StartAsync(detonateIED(grenade));
                    return false;
                }
            }
            return true;
        }
        private static IEnumerator detonateIED(Entity grenade)
        {
            grenade.PlaySound("ims_trigger");

            yield return Wait(1);

            if (grenade.HasField("cancel"))
            {
                grenade.ClearField("cancel");
                yield break;
            }

            grenade.Detonate();
            grenade.ClearField("owner");
        }
        private static IEnumerator watchForIEDEnd(Entity grenade)
        {
            yield return grenade.WaitTill("death");

            grenade.SetField("cancel", true);
        }
        #endregion

        #region Semtex
        private static IEnumerator waitForSemtexStuck(Entity grenade)
        {
            yield return grenade.WaitTill("missile_stuck");

            grenade.StopSound();
            grenade.Detonate();
        }
        #endregion

        #region Glowstick
        private static void dropGlowstick(Entity player, Vector3 position)
        {
            Entity glowstick = Spawn("script_model", position);
            glowstick.SetModel("viewmodel_light_stick");
            glowstick.Angles = player.Angles;
            Entity fx_friendly = SpawnFX(fx_glowstickGlow_green, glowstick.Origin);
            Entity fx_enemy = SpawnFX(fx_glowstickGlow_red, glowstick.Origin);
            fx_friendly.Angles = glowstick.Angles;
            fx_enemy.Angles = glowstick.Angles;
            TriggerFX(fx_friendly);
            TriggerFX(fx_enemy);
            fx_friendly.Hide();
            fx_enemy.Hide();

            Entity glowstickTrigger_friendly = Spawn("script_model", glowstick.Origin);
            glowstick.SetField("trigger_friendly", glowstickTrigger_friendly);
            glowstickTrigger_friendly.SetCursorHint("HINT_NOICON");
            glowstickTrigger_friendly.SetHintString("Press and hold ^3[{+activate}]^7 to pick up Glowstick");
            glowstickTrigger_friendly.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players != player)
                    glowstickTrigger_friendly.DisablePlayerUse(players);
                else
                    glowstickTrigger_friendly.EnablePlayerUse(players);
            }
            Entity glowstickTrigger_enemy = Spawn("script_model", glowstick.Origin);
            glowstick.SetField("trigger_enemy", glowstickTrigger_enemy);
            glowstickTrigger_enemy.SetCursorHint("HINT_NOICON");
            glowstickTrigger_enemy.SetHintString("Press and hold ^3[{+activate}]^7 to destroy Glowstick");
            glowstickTrigger_enemy.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players == player)
                    glowstickTrigger_enemy.DisablePlayerUse(players);
                else
                    glowstickTrigger_enemy.EnablePlayerUse(players);
            }

            foreach (Entity players in Players)
            {
                if (players.Classname != "player") continue;

                if (players.SessionTeam == player.SessionTeam)
                    fx_friendly.ShowToPlayer(players);
                else fx_enemy.ShowToPlayer(players);
            }

            glowstick.SetField("fx_friendly", fx_friendly);
            glowstick.SetField("fx_enemy", fx_enemy);
            glowstick.SetField("owner", player);

            player.SetField("glowstick", glowstick);
        }
        private static IEnumerator waitForGlowstickDrop(Entity player, Entity grenade)
        {
            Parameter[] returns = null;

            yield return grenade.WaitTill_return("missile_stuck", new Action<Parameter[]>((p) => returns = p));

            if (returns == null) yield break;

            Vector3 position = returns[0].As<Vector3>();
            dropGlowstick(player, position);
        }
        public static void spawnPlayerAtGlowstick(Entity player)
        {
            if (!player.HasField("glowstick")) return;

            Entity glowstick = player.GetField<Entity>("glowstick");
            player.SetOrigin(glowstick.Origin);
            player.SetPlayerAngles(glowstick.Angles);
            deleteGlowstick(glowstick);
        }
        private static void deleteGlowstick(Entity glowstick, Entity attacker = null)
        {
            glowstick.GetField<Entity>("fx_friendly").Delete();
            glowstick.GetField<Entity>("fx_enemy").Delete();
            glowstick.ClearField("fx_friendly");
            glowstick.ClearField("fx_enemy");
            glowstick.GetField<Entity>("trigger_friendly").Delete();
            glowstick.GetField<Entity>("trigger_enemy").Delete();
            glowstick.ClearField("trigger_friendly");
            glowstick.ClearField("trigger_enemy");

            glowstick.GetField<Entity>("owner").ClearField("glowstick");
            if (attacker != null)
            {
                glowstick.GetField<Entity>("owner").SetCardDisplaySlot(attacker, 5);
                glowstick.GetField<Entity>("owner").ShowHudSplash("destroyed_insertion", 1);
            }
            glowstick.ClearField("owner");

            if (glowsticks.Contains(glowstick)) glowsticks.Remove(glowstick);
            glowstick.Delete();
        }
        private static void pickupGlowstick(Entity player, Entity glowstick)
        {
            player.PlayLocalSound("chemlight_pu");

            player.GiveWeapon("lightstick_mp");
            deleteGlowstick(glowstick);
        }
        public static void checkForGlowstick(Entity player)
        {
            if (player.HasWeapon("flare_mp"))
            {
                player.TakeWeapon("flare_mp");
                player.GiveWeapon("lightstick_mp");
            }
        }
        #endregion

        #region Nine Bang
        private static IEnumerator waitForFlashExplode(Entity player, Entity grenade)
        {
            Parameter[] returns = null;

            yield return grenade.WaitTill_return("explode", new Action<Parameter[]>((p) => returns = p));

            if (returns == null) yield break;

            Vector3 position = returns[0].As<Vector3>();

            grenade.SetField("flashCount", 1);
            grenade.SetField("flashPosition", position);
            OnInterval(200, () => runNineBang(player, grenade));
        }
        private static bool runNineBang(Entity player, Entity grenade)
        {
            int flashCount = grenade.GetField<int>("flashCount");

            if (flashCount >= 12)
            {
                //Utilities.PrintToConsole("Flash end");
                grenade.ClearField("flashCount");
                return false;
            }
            else if (flashCount == 1 || flashCount % 4 == 0)
            {
                //Utilities.PrintToConsole("Flash break");
                grenade.SetField("flashCount", ++flashCount);
                return true;
            }

            grenade.SetField("flashCount", ++flashCount);
            Vector3 flashPosition = grenade.GetField<Vector3>("flashPosition");
            flashPosition = flashPosition.Around(80);
            PlaySoundAtPos(flashPosition, "flashbang_explode_default");
            PlayFX(fx_flashbang, flashPosition);

            foreach (Entity players in Players)//Flash any players nearby
            {
                if (players.Classname != "player" || !players.IsAlive) continue;

                if (isTeamBased && players.SessionTeam == player.SessionTeam) continue;

                float distance = 300 / players.GetEye().DistanceTo(flashPosition);

                if (distance >= 1) continue;

                distance *= .25f;
                float angle = players.SightConeTrace(flashPosition);
                players.Notify("flashbang", flashPosition, distance, angle, player, "free");
            }

            return true;
        }
        #endregion

        #region Frag
        private static bool runFragFire(Entity player, Entity attacker)
        {
            if (!player.IsAlive || player.GetStance() == "prone")
            {
                player.VisionSetNakedForPlayer("");
                return false;
            }

            player.PlayLocalSound("sentry_steam_body");
            player.FinishPlayerDamage(attacker, attacker, 5, 4, "MOD_TRIGGER_HURT", "barrel_mp", player.Origin, Vector3.Zero, "j_spine4", 0);
            updateDamageFeedback(attacker, "");
            player.ShellShock("dcburning", .75f);
            player.VisionSetNakedForPlayer("end_game", .1f);
            player.SetWaterSheeting(1, 2);
            PlayFX(fx_fire, player.GetTagOrigin("j_spine4"));

            if (!player.IsAlive)//Check again after
            {
                player.VisionSetNakedForPlayer("");
                return false;
            }
            return true;
        }
        #endregion

        #region stun
        private static bool waitForPlayerGrounded(Entity player)
        {
            if (!player.IsAlive) return false;

            if (player.IsOnGround())
            {
                player.SetStance("prone");
                return false;
            }
            return true;
        }
        #endregion

        #region nova gas
        private static IEnumerator waitForSmokeExplode(Entity player, Entity grenade)
        {
            Parameter[] returns = null;

            yield return grenade.WaitTill_return("explode", new Action<Parameter[]>((p) => returns = p));

            if (returns == null) yield break;

            Vector3 position = returns[0].As<Vector3>();
            createTearGas(player, position);
        }
        private static void createTearGas(Entity player, Vector3 origin)
        {
            Entity tearGasTrigger = Spawn("trigger_radius", origin, 0, 256, 128);
            tearGasTrigger.SetField("owner", player);
            OnInterval(50, () => watchGasTrigger(player, tearGasTrigger));
            AfterDelay(10000, () => tearGasTrigger.SetField("delete", true));
        }
        private static bool watchGasTrigger(Entity player, Entity gas)
        {
            if (gas.HasField("delete"))
            {
                gas.ClearField("delete");
                gas.ClearField("owner");
                gas.Delete();
                return false;
            }

            foreach (Entity players in Players)
            {
                if (players.Classname != "player" || !players.IsAlive) continue;

                //if (isTeamBased && players.SessionTeam == gas.GetField<Entity>("owner").SessionTeam) continue;
                //else if (!isTeamBased && players == gas.GetField<Entity>("owner")) continue;

                if (players.HasField("isInTearGas")) continue;
                if (!players.IsTouching(gas)) continue;

                players.SetField("isInTearGas", true);
                players.SetField("poison_gas", 0);
                OnInterval(1000, () => tearGasEffect(players, gas));
            }

            return true;
        }
        private static bool tearGasEffect(Entity player, Entity gas)
        {
            if (!player.IsAlive || player.Classname != "player" || !player.IsTouching(gas))
            {
                leaveGasTrigger(player);
                return false;
            }

            player.SetField("poison_gas", player.GetField<int>("poison_gas") + 1);

            switch (player.GetField<int>("poison_gas"))
            {
                case 1:
                    player.ShellShock("radiation_low", 4);
                    player.PlayLocalSound("weap_sniper_breathin");
                    break;
                case 2:
                    player.SetBlurForPlayer(1, 1);
                    break;
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                    player.SetBlurForPlayer(4, 2);
                    player.ShellShock("radiation_high", 2);
                    player.PlayLocalSound("breathing_hurt_alt");
                    doGasDamage(player, gas, 10);
                    break;
                case 8:
                    player.SetBlurForPlayer(8, 1);
                    player.SetWaterSheeting(2, 5);
                    player.ShellShock("radiation_high", 5);
                    player.PlayLocalSound("breathing_hurt_start_alt");
                    doGasDamage(player, gas, 20);
                    break;
            }

            return true;
        }
        private static void doGasDamage(Entity player, Entity gas, int damage)
        {
            player.FinishPlayerDamage(gas, gas.GetField<Entity>("owner"), damage, 0, "MOD_TRIGGER_HURT", "nuke_mp", player.Origin, Vector3.Zero, "none", 0);
            if (player != gas.GetField<Entity>("owner")) updateDamageFeedback(gas.GetField<Entity>("owner"), "");
        }
        private static void leaveGasTrigger(Entity player)
        {
            player.ClearField("poison_gas");
            player.ClearField("isInTearGas");

            AfterDelay(1000, () => player.PlayLocalSound("weap_sniper_breathgasp"));
            player.SetBlurForPlayer(0, 3);
        }
        #endregion

        #region Trophy
        private static Entity createTrophy(Entity player)
        {
            Entity trophy = Spawn("script_model", player.Origin);
            trophy.Angles = player.Angles;
            trophy.SetModel("mp_trophy_system");
            trophy.SetField("owner", player);
            trophy.Health = 999999;
            trophy.MaxHealth = 100;
            trophy.SetField("maxHealth", 100);
            trophy.SetField("damageTaken", 0);
            trophy.SetCanDamage(true);

            StartAsync(trophyDamageListener(trophy));

            Entity trophyTrigger = Spawn("script_model", trophy.Origin);
            trophy.SetField("trigger", trophyTrigger);
            trophyTrigger.SetCursorHint("HINT_NOICON");
            trophyTrigger.SetHintString("Press and hold ^3[{+activate}]^7 to pick up Radiator");
            trophyTrigger.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players != player)
                    trophyTrigger.DisablePlayerUse(players);
                else
                    trophyTrigger.EnablePlayerUse(players);
            }

            return trophy;
        }
        private static IEnumerator trophyDamageListener(Entity trophy)
        {
            Parameter[] returns = null;

            yield return trophy.WaitTill_return("damage", new Action<Parameter[]>((p) => returns = p));

            if (returns == null)
            {
                StartAsync(trophyDamageListener(trophy));
                yield break;
            }

            if (!trophy.HasField("owner"))
                yield break;//Abort

            //string modelName = (string)returns[5];
            //if (modelName != "mp_trophy_system")
            //yield break;//Abort

            int damage = (int)returns[0];
            Entity attacker = (Entity)returns[1];
            string type = (string)returns[4];
            string weapon = (string)returns[9];

            if (!attacker.IsPlayer)
            {
                StartAsync(trophyDamageListener(trophy));
                yield break;
            }

            if (isTeamBased && attacker.SessionTeam == trophy.GetField<Entity>("owner").SessionTeam && attacker != trophy.GetField<Entity>("owner"))
            {
                StartAsync(trophyDamageListener(trophy));
                yield break;
            }

            switch (weapon)
            {
                case "concussion_grenade_mp":
                case "flash_grenade_mp":
                case "smoke_grenade_mp":
                    StartAsync(trophyDamageListener(trophy));
                    yield break;
            }

            updateDamageFeedback(attacker, "");

            if (type == "MOD_MELEE")
                trophy.SetField("damageTaken", trophy.GetField<int>("maxHealth") + 1);

            trophy.SetField("damageTaken", trophy.GetField<int>("damageTaken") + damage);

            if (trophy.GetField<int>("damageTaken") >= trophy.GetField<int>("maxHealth"))
                StartAsync(trophyBreak(trophy));
            else StartAsync(trophyDamageListener(trophy));
        }
        private static void createTrophyRadiator(Entity player, Entity trophy)
        {
            if (!player.IsAlive) return;
            if (trophy.Model != "mp_trophy_system") return;

            Vector3 origin = trophy.GetTagOrigin("tag_dummy");
            Entity radiation = Spawn("trigger_radius", origin, 0, 128, 128);
            radiation.SetField("owner", player);
            radiation.SetField("trophy", trophy);
            trophy.SetField("radiation", radiation);

            Vector3 forward = AnglesToForward(trophy.Angles);
            Vector3 up = AnglesToUp(trophy.Angles);
            Entity fx = SpawnFX(fx_radiation, origin, up, forward);//Reversed angles to point upwards
            TriggerFX(fx);
            radiation.SetField("fx", fx);

            trophy.PlayLoopSound("emt_road_flare_burn");

            OnInterval(50, () => watchRadiationTrigger(player, radiation));
            OnInterval(250, () => runRadiatorFx(radiation));
        }
        private static bool watchRadiationTrigger(Entity player, Entity radiation)
        {
            if (!radiation.HasField("trophy"))
                return false;

            foreach (Entity players in Players)
            {
                if (players.Classname != "player" || !players.IsAlive) continue;

                if (isTeamBased && players.SessionTeam == radiation.GetField<Entity>("owner").SessionTeam) continue;
                else if (!isTeamBased && players == radiation.GetField<Entity>("owner")) continue;

                if (players.HasField("isInRadiation")) continue;
                if (!players.IsTouching(radiation)) continue;

                players.SetField("isInRadiation", true);
                players.SetField("poison", 0);
                OnInterval(1000, () => radiationEffect(players, radiation));
            }

            return true;
        }
        private static bool radiationEffect(Entity player, Entity radiation)
        {
            if (!player.IsAlive || player.Classname != "player" || !player.IsTouching(radiation))
            {
                leaveRadiationTrigger(player);
                return false;
            }

            player.SetField("poison", player.GetField<int>("poison") + 1);

            switch (player.GetField<int>("poison"))
            {
                case 1:
                    player.PlayLoopSound("item_geigercounter_level2");
                    break;
                case 3:
                    player.ShellShock("radiation_low", 4);
                    player.StopLoopSound();
                    player.PlayLoopSound("item_geigercounter_level3");
                    doRadiationDamage(player, radiation.GetField<Entity>("trophy"), 15);
                    break;
                case 4:
                    player.ShellShock("radiation_med", 5);
                    player.StopLoopSound();
                    player.PlayLoopSound("item_geigercounter_level3");
                    doRadiationDamage(player, radiation.GetField<Entity>("trophy"), 25);
                    break;
                case 6:
                    player.ShellShock("radiation_high", 4);
                    player.StopLoopSound();
                    player.PlayLoopSound("item_geigercounter_level4");
                    doRadiationDamage(player, radiation.GetField<Entity>("trophy"), 45);
                    break;
                case 8:
                    player.ShellShock("radiation_high", 5);
                    player.StopLoopSound();
                    player.PlayLoopSound("item_geigercounter_level4");
                    doRadiationDamage(player, radiation.GetField<Entity>("trophy"), 175);
                    break;
            }

            return true;
        }
        private static void doRadiationDamage(Entity player, Entity trophy, int damage)
        {
            player.FinishPlayerDamage(trophy, trophy.GetField<Entity>("owner"), damage, 0, "MOD_TRIGGER_HURT", "nuke_mp", player.Origin, Vector3.Zero, "none", 0);
            updateDamageFeedback(trophy.GetField<Entity>("owner"), "");
        }
        private static void leaveRadiationTrigger(Entity player)
        {
            player.ClearField("poison");
            player.ClearField("isInRadiation");
        }
        private static bool runRadiatorFx(Entity radiation)
        {
            if (!radiation.HasField("fx")) return false;

            Entity fx = radiation.GetField<Entity>("fx");
            TriggerFX(fx);

            return true;
        }
        private static void pickupTrophy(Entity player, Entity trophy)
        {
            player.PlayLocalSound("scavenger_pack_pickup");

            player.GiveWeapon("trophy_mp");
            deleteTrophy(trophy);
        }
        private static IEnumerator trophyBreak(Entity trophy)
        {
            PlayFXOnTag(fx_sentryExplode, trophy, "tag_origin");
            PlayFXOnTag(fx_sentrySmoke, trophy, "tag_origin");
            trophy.StopLoopSound();
            trophy.PlaySound("sentry_explode");

            trophy.GetField<Entity>("trigger").MakeUnUsable();
            trophy.GetField<Entity>("owner").ClearField("trophy");

            trophy.GetField<Entity>("radiation").ClearField("trophy");
            trophy.GetField<Entity>("radiation").ClearField("owner");
            trophy.GetField<Entity>("radiation").GetField<Entity>("fx").Delete();
            trophy.GetField<Entity>("radiation").ClearField("fx");
            trophy.GetField<Entity>("radiation").Delete();
            trophy.ClearField("radiation");

            yield return Wait(3);

            deleteTrophy(trophy);
        }
        private static void deleteTrophy(Entity trophy)
        {
            trophy.GetField<Entity>("owner").ClearField("trophy");
            trophy.ClearField("owner");
            if (trophy.HasField("radiation"))
            {
                trophy.GetField<Entity>("radiation").ClearField("trophy");
                trophy.GetField<Entity>("radiation").ClearField("owner");
                trophy.GetField<Entity>("radiation").GetField<Entity>("fx").Delete();
                trophy.GetField<Entity>("radiation").ClearField("fx");
                trophy.GetField<Entity>("radiation").Delete();
                trophy.ClearField("radiation");
            }
            trophy.GetField<Entity>("trigger").Delete();
            trophy.ClearField("trigger");

            trophy.Delete();
        }
        #endregion

        #region reflector
        private static IEnumerator waitForRadarStuck(Entity radar)
        {
            yield return radar.WaitTill("missile_stuck");

            createSolarReflector(radar.GetField<Entity>("owner"), radar.Origin, radar.Angles);
            radar.Delete();
        }
        private static Entity createSolarReflector(Entity player, Vector3 origin, Vector3 angles)
        {
            Entity reflector = Spawn("script_model", origin);
            reflector.Angles = angles;
            reflector.SetModel("weapon_radar");
            player.SetField("reflector", reflector);
            reflector.SetField("owner", player);
            reflector.Health = 999999;
            reflector.MaxHealth = 100;
            reflector.SetField("maxHealth", 100);
            reflector.SetField("damageTaken", 0);
            reflector.SetCanDamage(true);
            StartAsync(reflectorDamageListener(reflector));

            Entity reflectorTrigger = Spawn("script_model", reflector.Origin);
            reflector.SetField("trigger", reflectorTrigger);
            reflectorTrigger.SetCursorHint("HINT_NOICON");
            reflectorTrigger.SetHintString("Press and hold ^3[{+activate}]^7 to pick up Solar Reflector");
            reflectorTrigger.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players != player)
                    reflectorTrigger.DisablePlayerUse(players);
                else
                    reflectorTrigger.EnablePlayerUse(players);
            }

            /*
            if (isTeamBased)
            {
                HudElem reflection = NewTeamHudElem(otherTeam[player.SessionTeam]);
                reflection.AlignX = HudElem.XAlignments.Center;
                reflection.AlignY = HudElem.YAlignments.Middle;
                reflection.HorzAlign = HudElem.HorzAlignments.Fullscreen;
                reflection.VertAlign = HudElem.VertAlignments.Fullscreen;
                reflection.Alpha = .9f;
                reflection.Archived = false;
                reflection.Foreground = true;
                reflection.HideIn3rdPerson = false;
                reflection.HideWhenDead = false;
                reflection.HideWhenInDemo = false;
                reflection.HideWhenInMenu = false;
                reflection.LowResBackground = false;
                reflection.Sort = 10000;
                //reflection.SetShader("gfx_glow_hs_extreme", 640, 480);
                reflection.X = origin.X;
                reflection.Y = origin.Y;
                reflection.Z = origin.Z;
            }
            else
            */
            {
                foreach (Entity players in Players)
                {
                    if (players.Classname != "player") continue;
                    //if (players == player) continue;

                    createReflectorShader(players, reflector);
                }
            }

            return reflector;
        }
        private static void createReflectorShader(Entity player, Entity reflector)
        {
            HudElem reflection = NewClientHudElem(player);
            reflection.Archived = true;
            reflection.X = reflector.Origin.X;
            reflection.Y = reflector.Origin.Y;
            reflection.Z = reflector.Origin.Z;
            reflection.Alpha = 1f;
            reflection.SetShader(fx_flare, 640, 480);
            reflection.SetWaypoint(false, false, true, false);

            player.SetClientDvars("waypointIconHeight", 1200, "waypointIconWidth", 1200);
            OnInterval(50, () => monitorReflectorVisibility(player, reflector, reflection));
        }
        private static IEnumerator reflectorDamageListener(Entity reflector)
        {
            Parameter[] returns = null;

            yield return reflector.WaitTill_return("damage", new Action<Parameter[]>((p) => returns = p));

            if (returns == null)
            {
                StartAsync(reflectorDamageListener(reflector));
                yield break;
            }

            if (!reflector.HasField("owner"))
                yield break;//Abort

            //string modelName = (string)returns[5];
            //if (modelName != "mp_trophy_system")
            //yield break;//Abort

            int damage = (int)returns[0];
            Entity attacker = (Entity)returns[1];
            string type = (string)returns[4];
            string weapon = (string)returns[9];

            if (!attacker.IsPlayer)
            {
                StartAsync(reflectorDamageListener(reflector));
                yield break;
            }

            if (isTeamBased && attacker.SessionTeam == reflector.GetField<Entity>("owner").SessionTeam && attacker != reflector.GetField<Entity>("owner"))
            {
                StartAsync(reflectorDamageListener(reflector));
                yield break;
            }

            switch (weapon)
            {
                case "concussion_grenade_mp":
                case "flash_grenade_mp":
                case "smoke_grenade_mp":
                    StartAsync(reflectorDamageListener(reflector));
                    yield break;
            }

            updateDamageFeedback(attacker, "");

            if (type == "MOD_MELEE")
                reflector.SetField("damageTaken", reflector.GetField<int>("maxHealth") + 1);

            reflector.SetField("damageTaken", reflector.GetField<int>("damageTaken") + damage);

            if (reflector.GetField<int>("damageTaken") >= reflector.GetField<int>("maxHealth"))
                StartAsync(reflectorBreak(reflector));
            else StartAsync(reflectorDamageListener(reflector));
        }
        private static bool monitorReflectorVisibility(Entity player, Entity reflector, HudElem reflection)
        {
            if (/*!player.IsAlive || */player.Classname != "player" || !reflector.HasField("owner"))
            {
                reflection.Destroy();
                return false;
            }

            float visibility = reflector.SightConeTrace(player.GetEye(), player);
            int viewVisibility = player.WorldPointInReticle_Circle(reflector.Origin, 135, 135) ? 1 : 0;

            reflection.Alpha = visibility * viewVisibility;

            return true;
        }
        private static void pickupReflector(Entity player, Entity reflector)
        {
            player.PlayLocalSound("scavenger_pack_pickup");

            player.GiveWeapon("portable_radar_mp");
            deleteReflector(reflector);
        }

        private static IEnumerator reflectorBreak(Entity reflector)
        {
            PlayFX(fx_reflectorSparks, reflector.Origin);
            reflector.PlaySound("sentry_explode");

            reflector.GetField<Entity>("trigger").MakeUnUsable();
            reflector.GetField<Entity>("owner").ClearField("reflector");
            reflector.ClearField("owner");

            yield return Wait(1);

            deleteReflector(reflector);
        }
        private static void deleteReflector(Entity reflector)
        {
            reflector.GetField<Entity>("trigger").Delete();
            reflector.ClearField("trigger");
            if (reflector.HasField("owner"))
            {
                reflector.GetField<Entity>("owner").ClearField("reflector");
                reflector.ClearField("owner");
            }

            reflector.Delete();
        }
        #endregion

        #region radio
        private static Entity createRadioPlayer(Entity player)
        {
            Entity radio = Spawn("script_model", player.Origin);
            radio.Angles = player.Angles;
            radio.SetModel("weapon_jammer");
            radio.SetField("owner", player);
            radio.Health = 999999;
            radio.MaxHealth = 100;
            radio.SetField("maxHealth", 100);
            radio.SetField("damageTaken", 0);
            radio.SetCanDamage(true);
            StartAsync(radioDamageListener(radio));

            player.SetField("radio", radio);

            Entity radioTrigger = Spawn("script_model", radio.Origin);
            radio.SetField("trigger", radioTrigger);
            radioTrigger.SetCursorHint("HINT_NOICON");
            radioTrigger.SetHintString("Press and hold ^3[{+activate}]^7 to pick up Loudspeaker");
            radioTrigger.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players != player)
                    radioTrigger.DisablePlayerUse(players);
                else
                    radioTrigger.EnablePlayerUse(players);
            }

            StartAsync(playRadioNoise(radio));

            return radio;
        }
        private static IEnumerator radioDamageListener(Entity radio)
        {
            Parameter[] returns = null;

            yield return radio.WaitTill_return("damage", new Action<Parameter[]>((p) => returns = p));

            if (returns == null)
            {
                StartAsync(radioDamageListener(radio));
                yield break;
            }

            if (!radio.HasField("owner"))
                yield break;//Abort

            //string modelName = (string)returns[5];
            //if (modelName != "mp_trophy_system")
            //yield break;//Abort

            int damage = (int)returns[0];
            Entity attacker = (Entity)returns[1];
            string type = (string)returns[4];
            string weapon = (string)returns[9];

            if (!attacker.IsPlayer)
            {
                StartAsync(radioDamageListener(radio));
                yield break;
            }

            if (isTeamBased && attacker.SessionTeam == radio.GetField<Entity>("owner").SessionTeam && attacker != radio.GetField<Entity>("owner"))
            {
                StartAsync(radioDamageListener(radio));
                yield break;
            }

            switch (weapon)
            {
                case "concussion_grenade_mp":
                case "flash_grenade_mp":
                case "smoke_grenade_mp":
                    StartAsync(radioDamageListener(radio));
                    yield break;
            }

            updateDamageFeedback(attacker, "");

            if (type == "MOD_MELEE")
                radio.SetField("damageTaken", radio.GetField<int>("maxHealth") + 1);

            radio.SetField("damageTaken", radio.GetField<int>("damageTaken") + damage);

            if (radio.GetField<int>("damageTaken") >= radio.GetField<int>("maxHealth"))
                StartAsync(radioBreak(radio));
            else StartAsync(radioDamageListener(radio));
        }
        private static IEnumerator playRadioNoise(Entity radio)
        {
            if (!radio.HasField("owner"))
                yield break;

            yield return Wait(RandomFloatRange(1, 8));

            int randomSound = RandomInt(radioNoises.Count);
            int volume = RandomIntRange(1, 3);

            for (int i = 0; i < volume; i++)
                radio.PlaySound(radioNoises[randomSound]);

            PlayFXOnTag(fx_radioLight, radio, "tag_fx");
            AfterDelay(950, () => StopFXOnTag(fx_radioLight, radio, "tag_fx"));

            StartAsync(playRadioNoise(radio));
        }
        private static void pickupRadio(Entity player, Entity radio)
        {
            player.PlayLocalSound("scavenger_pack_pickup");

            player.GiveWeapon("scrambler_mp");
            deleteRadio(radio);
        }
        private static IEnumerator radioBreak(Entity radio)
        {
            PlayFXOnTag(fx_sentryExplode, radio, "tag_origin");
            PlayFXOnTag(fx_sentrySmoke, radio, "tag_origin");
            radio.PlaySound("sentry_explode");

            radio.GetField<Entity>("trigger").MakeUnUsable();
            radio.GetField<Entity>("owner").ClearField("radio");
            radio.ClearField("owner");

            yield return Wait(3);

            deleteRadio(radio);
        }
        private static void deleteRadio(Entity radio)
        {
            radio.GetField<Entity>("trigger").Delete();
            radio.ClearField("trigger");
            if (radio.HasField("owner"))
            {
                radio.GetField<Entity>("owner").ClearField("radio");
                radio.ClearField("owner");
            }

            radio.Delete();
        }
        #endregion

        #region shockwave
        private static IEnumerator waitForBettyStuck(Entity radar)
        {
            yield return radar.WaitTill("missile_stuck");

            createShockwaveGrenade(radar.GetField<Entity>("owner"), radar.Origin, radar.Angles);
            radar.Delete();
        }
        private static Entity createShockwaveGrenade(Entity player, Vector3 origin, Vector3 angles)
        {
            Entity shockwave = Spawn("script_model", origin);
            shockwave.Angles = angles;
            shockwave.SetModel("projectile_bouncing_betty_grenade");
            shockwave.Health = 999999;
            shockwave.MaxHealth = 100;
            shockwave.SetField("maxHealth", 100);
            shockwave.SetField("damageTaken", 0);
            shockwave.SetCanDamage(true);
            shockwave.SetField("owner", player);
            player.SetField("shockwave", shockwave);

            Entity shockwaveTrigger = Spawn("script_model", shockwave.Origin);
            shockwave.SetField("trigger", shockwaveTrigger);
            shockwaveTrigger.SetCursorHint("HINT_NOICON");
            shockwaveTrigger.SetHintString("Press and hold ^3[{+activate}]^7 to pick up Shockwave Mine");
            shockwaveTrigger.MakeUsable();
            foreach (Entity players in Players)
            {
                if (players != player)
                    shockwaveTrigger.DisablePlayerUse(players);
                else
                    shockwaveTrigger.EnablePlayerUse(players);
            }

            StartAsync(shockwaveDamageListener(shockwave));

            OnInterval(50, () => watchShockwave(shockwave));

            return shockwave;
        }
        private static IEnumerator shockwaveDamageListener(Entity grenade)
        {
            Parameter[] returns = null;

            yield return grenade.WaitTill_return("damage", new Action<Parameter[]>((p) => returns = p));

            if (returns == null)
            {
                StartAsync(shockwaveDamageListener(grenade));
                yield break;
            }

            if (!grenade.HasField("owner"))
                yield break;//Abort

            Entity attacker = (Entity)returns[1];
            string weapon = (string)returns[9];

            if (!attacker.IsPlayer)
            {
                StartAsync(shockwaveDamageListener(grenade));
                yield break;
            }

            if (isTeamBased && attacker.SessionTeam == grenade.GetField<Entity>("owner").SessionTeam && attacker != grenade.GetField<Entity>("owner"))
            {
                StartAsync(shockwaveDamageListener(grenade));
                yield break;
            }

            switch (weapon)
            {
                case "concussion_grenade_mp":
                case "flash_grenade_mp":
                case "smoke_grenade_mp":
                    StartAsync(shockwaveDamageListener(grenade));
                    yield break;
            }

            updateDamageFeedback(attacker, "");

            StartAsync(shockwaveExplode(grenade));
        }
        private static bool watchShockwave(Entity grenade)
        {
            if (grenade.GetField<Entity>("owner").Classname != "player" || !grenade.GetField<Entity>("owner").IsAlive) return false;
            if (!grenade.HasField("owner"))
                return false;

            foreach (Entity players in Players)
            {
                if (players.Classname != "player" || !players.IsAlive) continue;
                //if (isTeamBased && players.SessionTeam == grenade.GetField<Entity>("owner").SessionTeam) continue;
                //else if (!isTeamBased && players == grenade.GetField<Entity>("owner")) continue;

                if (players.Origin.DistanceTo(grenade.Origin) < 128)
                {
                    grenade.PlaySound("mine_betty_click");
                    grenade.GetField<Entity>("trigger").Delete();
                    grenade.ClearField("trigger");
                    grenade.GetField<Entity>("owner").ClearField("shockwave");
                    grenade.SetCanDamage(false);
                    AfterDelay(1000, () => StartAsync(launchShockwave(grenade)));
                    return false;
                }
            }
            return true;
        }
        private static IEnumerator launchShockwave(Entity grenade)
        {
            grenade.PlaySound("mine_betty_spin");
            PlayFX(fx_mineLaunch, grenade.Origin);
            grenade.SetField("poundPos", grenade.Origin);

            Vector3 shockPos = grenade.Origin + new Vector3(0, 0, 96);
            grenade.MoveTo(shockPos, .7f, 0, .65f);
            grenade.RotateVelocity(new Vector3(0, 750, 32), .7f, 0, .65f);

            //playSpinnerFX(grenade);

            yield return Wait(.65f);

            StartAsync(shockwavePound(grenade));
        }
        private static IEnumerator shockwavePound(Entity grenade)
        {
            grenade.PlaySound("ims_launch");
            grenade.MoveTo(grenade.GetField<Vector3>("poundPos"), .3f);

            yield return Wait(.3f);

            grenade.PlaySound("explo_tree_layer");
            PlayFXOnTag(fx_mineExplode, grenade, "tag_fx");
            PlayFX(fx_radiation, grenade.Origin);

            yield return WaitForFrame();

            grenade.Hide();
            grenade.RadiusDamage(grenade.Origin, 128, 150, 80, grenade.GetField<Entity>("owner"), "MOD_EXPLOSIVE", "bouncingbetty_mp");
            Earthquake(.5f, .5f, grenade.Origin, 192);
            PhysicsExplosionSphere(grenade.Origin, 192, 192, 2);

            foreach (Entity players in Players)
            {
                if (players.Classname != "player" || !players.IsAlive) continue;
                //if (isTeamBased && players.SessionTeam == grenade.GetField<Entity>("owner").SessionTeam) continue;
                //else if (!isTeamBased && players == grenade.GetField<Entity>("owner")) continue;

                if (players.Origin.DistanceTo(grenade.Origin) < 192)
                    players.ShellShock("dog_bite", 3);
            }

            yield return Wait(.2f);

            grenade.ClearField("owner");
            grenade.Delete();
        }
        private static void pickupShockwave(Entity player, Entity shockwave)
        {
            player.PlayLocalSound("scavenger_pack_pickup");

            player.GiveWeapon("bouncingbetty_mp");
            deleteShockwave(shockwave);
        }
        private static IEnumerator shockwaveExplode(Entity grenade)
        {
            grenade.PlaySound("grenade_explode_metal");
            grenade.RadiusDamage(grenade.Origin, 128, 150, 80, grenade.GetField<Entity>("owner"), "MOD_EXPLOSIVE", "bouncingbetty_mp");
            PlayFXOnTag(fx_mineExplode, grenade, "tag_fx");

            yield return Wait(.2f);

            grenade.ClearField("owner");
            grenade.Delete();
        }
        private static void deleteShockwave(Entity grenade)
        {
            grenade.GetField<Entity>("owner").ClearField("shockwave");
            grenade.ClearField("owner");
            grenade.GetField<Entity>("trigger").Delete();
            grenade.ClearField("trigger");
            grenade.Delete();
        }
        #endregion

        #region QED
        private static IEnumerator primeQED(Entity player, Entity grenade)
        {
            grenade.PlaySound("mine_betty_spin");

            yield return Wait(.85f);//Wait until one frame before EMP explosion

            Vector3 origin = grenade.Origin;
            grenade.Delete();

            detonateQED(player, origin);
        }
        private static void detonateQED(Entity player, Vector3 origin)
        {
            PlayFX(fx_empExplode, origin);
            PlaySoundAtPos(origin, "emp_grenade_detonate");

            int random = RandomInt(21);

            switch (random)
            {
                case 0:
                    qed_downNearbyPlayers(origin);
                    break;
                case 5:
                    qed_dropPerk(origin);
                    break;
                case 10:
                    StartAsync(qed_fireWeaponInCircle(player, origin, qed_getWeaponForCircleFire()));
                    break;
                case 15:
                    qed_teleportPlayersNearby(origin);
                    break;
                case 20:
                    qed_giveMaxAmmoToNearbyPlayers(origin);
                    break;
                default:
                    qed_physicsPush(origin);
                    break;
            }
        }
        private static string qed_getWeaponForCircleFire()
        {
            int random = RandomInt(20);

            switch (random)
            {
                case 0:
                case 1:
                case 2:
                   return "ac130_20mm_mp";
                case 3:
                case 4:
                    return "iw5_spas12_mp";
                case 5:
                case 6:
                    return "gl_mp";
                case 7:
                case 8:
                    return "iw5_smaw_mp";
                case 9:
                    return "iw5_44magnum_mp";
                default:
                    return "iw5_deserteagle_mp";
            }
        }
        private static void qed_teleportPlayersNearby(Vector3 origin)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.Classname != "player") continue;
                if (player.Origin.DistanceTo(origin) > 256) continue;

                Entity randomSpawn = qed_getRandomSpawn();
                player.SetOrigin(randomSpawn.Origin);
                player.SetPlayerAngles(randomSpawn.Angles);
            }
        }
        private static void qed_giveMaxAmmoToNearbyPlayers(Vector3 origin)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.Classname != "player") continue;
                if (player.Origin.DistanceTo(origin) > 256) continue;

                player.GiveMaxAmmo(player.CurrentWeapon);
            }
        }
        private static void qed_downNearbyPlayers(Vector3 origin)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.Classname != "player") continue;
                if (player.Origin.DistanceTo(origin) > 128) continue;

                player.SetPerk("specialty_pistoldeath", true, true);
                player.FinishPlayerDamage(null, null, player.Health, 0, "MOD_FALLING", "emp_grenade_mp", Vector3.Zero, Vector3.Zero, "", 0);
            }
        }
        private static Entity qed_getRandomSpawn()
        {
            Entity ret = null;
            for (int i = 0; i < 1000; i++)
            {
                Entity e = Entity.GetEntity(i);
                if (e == null) continue;
                if (e.Classname == "mp_dm_spawn")
                {
                    ret = e;
                    if (RandomInt(100) > 50) break;
                }
                else continue;
            }
            return ret;
        }
        private static IEnumerator qed_fireWeaponInCircle(Entity player, Vector3 origin, string weaponName)
        {
            Entity weapon = Spawn("script_model", origin);
            weapon.SetModel(GetWeaponModel(weaponName));
            weapon.MoveTo(origin + new Vector3(0, 0, 40), 1);

            yield return Wait(1);

            weapon.RotateYaw(360, 6);

            int time = 0;
            OnInterval(300, () =>
            {
                time++;

                Vector3 flashPos = weapon.GetTagOrigin("tag_flash");
                Vector3 flashAngles = weapon.GetTagAngles("tag_flash");
                if (weapon.Model == "weapon_m16") flashPos = weapon.GetTagOrigin("tag_flash_silenced");

                Vector3 forward = AnglesToForward(flashAngles);
                Vector3 targetPos = forward * 1000000;
                MagicBullet(weaponName, flashPos, targetPos, player);

                int fx = WeaponClass(weaponName) == "spread" ? fx_tracer_shotgun : fx_tracer_single;
                Vector3 up = AnglesToUp(flashAngles);
                if (weaponName != "iw5_smaw_mp") PlayFX(fx, flashPos, forward); 

                if (time == 20)
                {
                    weapon.Delete();
                    return false;
                }
                return true;
            });
        }
        private static void qed_physicsPush(Vector3 origin)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.Classname != "player") continue;

                if (player.Origin.DistanceTo(origin) < 256)
                {
                    Vector3 angle = VectorToAngles(player.Origin - origin);
                    angle = AnglesToForward(angle);
                    angle.Normalize();
                    player.SetVelocity(new Vector3(angle.X * 200, angle.Y * 200, 100));
                }
            }
        }
        private static void qed_dropPerk(Vector3 origin)
        {
            Entity perk = Spawn("script_model", origin + new Vector3(0, 0, 20));
            perk.SetModel("viewmodel_uav_radio");
            perk.Angles = new Vector3(15, 15, 15);
            perk.SetField("lifetime", 200);

            perk.RotateYaw(360, 10);

            OnInterval(50, () => watchPerkPickup(perk));
        }
        private static bool watchPerkPickup(Entity perk)
        {
            foreach (Entity player in Players)
            {
                if (!player.IsAlive || player.Classname != "player") continue;
                if (player.Origin.DistanceTo(perk.Origin) > 64) continue;

                qed_givePlayerRandomPerk(player, perk.Origin);
                perk.Delete();
                return false;
            }

            perk.SetField("lifetime", perk.GetField<int>("lifetime") - 1);

            if (perk.GetField<int>("lifetime") <= 0)
            {
                perk.Delete();
                return false;
            }

            return true;
        }
        private static void qed_givePlayerRandomPerk(Entity player, Vector3 origin)
        {
            int random = RandomInt(20);

            switch (random)
            {
                case 0:
                    player.SetPerk("specialty_quieter", true, true);
                    break;
                case 1:
                    player.SetPerk("specialty_longersprint", true, true);
                    break;
                case 2:
                    player.SetPerk("specialty_bulletaccuracy", true, true);
                    break;
                case 3:
                    player.SetPerk("specialty_rof", true, true);
                    break;
                case 4:
                    player.SetPerk("specialty_fastreload", true, true);
                    break;
                case 5:
                    player.SetPerk("specialty_jumpdive", true, true);
                    break;
                case 6:
                    player.SetPerk("specialty_fastmantle", true, true);
                    break;
                case 7:
                    player.SetPerk("specialty_explosivebullets", true, true);
                    break;
                case 8:
                    player.SetPerk("specialty_thermal", true, true);
                    break;
                case 9:
                    player.SetPerk("specialty_quickdraw", true, true);
                    break;
                case 10:
                    player.SetPerk("specialty_extendedmags", true, true);
                    break;
                case 11:
                    player.SetPerk("specialty_marathon", true, true);
                    break;
                case 12:
                    player.SetPerk("specialty_extendedmelee", true, true);
                    break;
                case 13:
                    player.SetPerk("specialty_automantle", true, true);
                    break;
                case 14:
                    player.SetPerk("specialty_fmj", true, true);
                    break;
                case 15:
                    player.SetPerk("specialty_lowprofile", true, true);
                    break;
                case 16:
                    player.SetPerk("specialty_stalker", true, true);
                    break;
                case 17:
                    player.SetPerk("specialty_fastermelee", true, true);
                    break;
                default:
                    break;
            }

            player.OpenMenu("perk_display");
            player.PlayLocalSound("earn_perk");
            PlayFX(fx_disappear, origin);
        }
        #endregion

        #region trip mines
        #endregion

        #region knife
        private static void checkForKnifeGrab(Entity player, Entity attacker, Entity inflictor)
        {
            bool trace = SightTracePassed(attacker.GetEye(), player.GetEye(), false, attacker);
            if (trace)
                StartAsync(rappelPlayerToAttacker(player, attacker));
            else
                player.FinishPlayerDamage(inflictor, attacker, player.Health, 0, "MOD_PROJECTILE", "throwingknife_mp", player.Origin, Vector3.Zero, "", 0);
        }
        private static IEnumerator rappelPlayerToAttacker(Entity player, Entity attacker)
        {
            Vector3 anglesToAttacker = VectorToAngles(attacker.Origin - player.Origin);
            Vector3 anglesToPlayer = VectorToAngles(player.Origin - attacker.Origin); 

            Entity playerHelper = Spawn("script_model", player.Origin);
            playerHelper.SetModel("tag_origin");
            playerHelper.Angles = anglesToAttacker;
            player.PlayerLinkToBlend(playerHelper, "tag_origin", 0, 0, 0, 0, 0, false);

            Entity attackerHelper = Spawn("script_model", attacker.Origin);
            attackerHelper.SetModel("tag_origin");
            attackerHelper.Angles = anglesToPlayer;
            attacker.PlayerLinkToBlend(attackerHelper, "tag_origin", 0, 0, 0, 0, 0, false);

            Vector3 endPos = attacker.Origin;

            player.DisableWeapons();
            player.DisableUsability();
            player.DisableOffhandWeapons();

            attacker.SetField("lastWeapon", attacker.CurrentWeapon);
            attacker.GiveWeapon("stealth_bomb_mp");
            attacker.SwitchToWeapon("stealth_bomb_mp");
            attacker.DisableWeaponSwitch();

            float distance = player.Origin.DistanceTo(endPos);
            playerHelper.MoveTo(endPos, distance / 500);
            player.PlayLocalSound("ui_camera_whoosh_in");
            player.SetBlurForPlayer(2f, .2f);

            yield return Wait(distance / 500);

            player.SetBlurForPlayer(0, 1);
            player.Unlink();
            attacker.Unlink();
            playerHelper.Delete();
            attackerHelper.Delete();
            attacker.SwitchToWeapon(attacker.GetField<string>("lastWeapon"));
            attacker.EnableWeaponSwitch();
            player.FinishPlayerDamage(attacker, attacker, player.Health, 0, "MOD_MELEE", "throwingknife_mp", player.Origin, Vector3.Zero, "", 0);
            PhysicsExplosionCylinder(attacker.Origin, 64, 64, 25);
        }
        #endregion
    }
}
