using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Broms.RoyalTransportTaxiService
{
	// Token: 0x02000E18 RID: 3608
	[StaticConstructorOnStartup]
	public class Broms.RoyalTitlePermitWorker_CallShuttle : RoyalTitlePermitWorker_Targeted
	{
		// Token: 0x06005440 RID: 21568 RVA: 0x001C91D0 File Offset: 0x001C73D0
		public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
		{
			if (!base.CanHitTarget(target))
			{
				if (target.IsValid && showMessages)
				{
					Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
				}
				return false;
			}
			AcceptanceReport acceptanceReport = Broms.RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, this.map);
			if (!acceptanceReport.Accepted)
			{
				Messages.Message(acceptanceReport.Reason, new LookTargets(target.Cell, this.map), MessageTypeDefOf.RejectInput, false);
			}
			return acceptanceReport.Accepted;
		}

		// Token: 0x06005441 RID: 21569 RVA: 0x001C9268 File Offset: 0x001C7468
		public override void DrawHighlight(LocalTargetInfo target)
		{
			GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
			Broms.RoyalTitlePermitWorker_CallShuttle.DrawShuttleGhost(target, this.map);
		}

		// Token: 0x06005442 RID: 21570 RVA: 0x001C929C File Offset: 0x001C749C
		public override void OrderForceTarget(LocalTargetInfo target)
		{
			this.CallShuttle(target.Cell);
		}

		// Token: 0x06005443 RID: 21571 RVA: 0x001C92AC File Offset: 0x001C74AC
		public override void OnGUI(LocalTargetInfo target)
		{
			if (!target.IsValid || !Broms.RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, this.map).Accepted)
			{
				GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
			}
		}

		// Token: 0x06005444 RID: 21572 RVA: 0x001C92E2 File Offset: 0x001C74E2
		public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
		{
			if (faction.HostileTo(Faction.OfPlayer))
			{
				yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
				yield break;
			}
			string label = this.def.LabelCap + ": ";
			Action action = null;
			bool free;
			if (base.FillAidOption(pawn, faction, ref label, out free))
			{
				action = delegate()
				{
					this.BeginCallShuttle(pawn, pawn.MapHeld, faction, free);
				};
			}
			yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
			yield break;
		}

		// Token: 0x06005445 RID: 21573 RVA: 0x001C9300 File Offset: 0x001C7500
		public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
		{
			string defaultDesc;
			bool flag;
			if (!base.FillCaravanAidOption(pawn, faction, out defaultDesc, out this.free, out flag))
			{
				yield break;
			}
			Command_Action command_Action = new Command_Action
			{
				defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
				defaultDesc = defaultDesc,
				icon = Broms.RoyalTitlePermitWorker_CallShuttle.CommandTex,
				action = delegate()
				{
					this.CallShuttleToCaravan(pawn, faction, this.free);
				}
			};
			if (faction.HostileTo(Faction.OfPlayer))
			{
				command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
			}
			if (flag)
			{
				command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
			}
			yield return command_Action;
			yield break;
		}

		// Token: 0x06005446 RID: 21574 RVA: 0x001C9320 File Offset: 0x001C7520
		private void BeginCallShuttle(Pawn caller, Map map, Faction faction, bool free)
		{
			this.targetingParameters = new TargetingParameters();
			this.targetingParameters.canTargetLocations = true;
			this.targetingParameters.canTargetSelf = false;
			this.targetingParameters.canTargetPawns = false;
			this.targetingParameters.canTargetFires = false;
			this.targetingParameters.canTargetBuildings = true;
			this.targetingParameters.canTargetItems = true;
			this.targetingParameters.validator = ((TargetInfo target) => this.def.royalAid.targetingRange <= 0f || target.Cell.DistanceTo(caller.Position) <= this.def.royalAid.targetingRange);
			this.caller = caller;
			this.map = map;
			this.calledFaction = faction;
			this.free = free;
			Find.Targeter.BeginTargeting(this, null);
		}

		// Token: 0x06005447 RID: 21575 RVA: 0x001C93DC File Offset: 0x001C75DC
		private void CallShuttle(IntVec3 landingCell)
		{
			if (this.caller.Spawned)
			{
				Thing thing = ThingMaker.MakeThing(ThingDefOf.Shuttle, null);
				thing.TryGetComp<CompShuttle>().permitShuttle = true;
				TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, thing);
				transportShip.ArriveAt(landingCell, this.map.Parent);
				transportShip.AddJobs(new ShipJobDef[]
				{
					ShipJobDefOf.WaitForever,
					ShipJobDefOf.Unload,
					ShipJobDefOf.FlyAway
				});
				this.caller.royalty.GetPermit(this.def, this.calledFaction).Notify_Used();
				if (!this.free)
				{
					this.caller.royalty.TryRemoveFavor(this.calledFaction, this.def.royalAid.favorCost);  // use this code section to search for silver cost eventually
				}
			}
		}

		// Token: 0x06005448 RID: 21576 RVA: 0x001C94A4 File Offset: 0x001C76A4
		private void CallShuttleToCaravan(Pawn caller, Faction faction, bool free)
		{
			Broms.RoyalTitlePermitWorker_CallShuttle.<>c__DisplayClass10_0 CS$<>8__locals1 = new Broms.RoyalTitlePermitWorker_CallShuttle.<>c__DisplayClass10_0();
			CS$<>8__locals1.caller = caller;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.faction = faction;
			CS$<>8__locals1.free = free;
			CS$<>8__locals1.caravan = CS$<>8__locals1.caller.GetCaravan();
			CS$<>8__locals1.maxLaunchDistance = (int)this.def.royalAid.targetingRange;
			CameraJumper.TryJump(CameraJumper.GetWorldTarget(CS$<>8__locals1.caravan));
			Find.WorldSelector.ClearSelection();
			CS$<>8__locals1.caravanTile = CS$<>8__locals1.caravan.Tile;
			Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(CS$<>8__locals1.<CallShuttleToCaravan>g__ChoseWorldTarget|1), true, CompLaunchable.TargeterMouseAttachment, false, delegate
			{
				if (CS$<>8__locals1.maxLaunchDistance > 0)
				{
					GenDraw.DrawWorldRadiusRing(CS$<>8__locals1.caravanTile, CS$<>8__locals1.maxLaunchDistance);
				}
			}, (GlobalTargetInfo target) => CompLaunchable.TargetingLabelGetter(target, CS$<>8__locals1.caravanTile, CS$<>8__locals1.maxLaunchDistance, Gen.YieldSingle<Caravan>(CS$<>8__locals1.caravan), new Action<int, TransportPodsArrivalAction>(base.<CallShuttleToCaravan>g__Launch|0), null), null);
		}

		// Token: 0x06005449 RID: 21577 RVA: 0x001C9564 File Offset: 0x001C7764
		public static void DrawShuttleGhost(LocalTargetInfo target, Map map)
		{
			Color ghostCol = Broms.RoyalTitlePermitWorker_CallShuttle.ShuttleCanLandHere(target, map).Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
			GhostDrawer.DrawGhostThing(target.Cell, Rot4.North, ThingDefOf.Shuttle, ThingDefOf.Shuttle.graphic, ghostCol, AltitudeLayer.Blueprint, null, true, null);
			Vector3 position = (target.Cell + IntVec3.South * 2).ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint);
			Graphics.DrawMesh(MeshPool.plane10, position, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
		}

		// Token: 0x0600544A RID: 21578 RVA: 0x001C95F0 File Offset: 0x001C77F0
		public static AcceptanceReport ShuttleCanLandHere(LocalTargetInfo target, Map map)
		{
			TaggedString t = "CannotCallShuttle".Translate() + ": ";
			if (!target.IsValid)
			{
				return new AcceptanceReport(t + "MessageTransportPodsDestinationIsInvalid".Translate().CapitalizeFirst());
			}
			foreach (IntVec3 cell in GenAdj.OccupiedRect(target.Cell, Rot4.North, ThingDefOf.Shuttle.size))
			{
				string reportFromCell = Broms.RoyalTitlePermitWorker_CallShuttle.GetReportFromCell(cell, map, false);
				if (reportFromCell != null)
				{
					return new AcceptanceReport(t + reportFromCell);
				}
			}
			string reportFromCell2 = Broms.RoyalTitlePermitWorker_CallShuttle.GetReportFromCell(target.Cell + ShipJob_Unload.DropoffSpotOffset, map, true);
			if (reportFromCell2 != null)
			{
				return new AcceptanceReport(t + reportFromCell2);
			}
			return AcceptanceReport.WasAccepted;
		}

		// Token: 0x0600544B RID: 21579 RVA: 0x001C96F0 File Offset: 0x001C78F0
		private static string GetReportFromCell(IntVec3 cell, Map map, bool interactionSpot)
		{
			if (!cell.InBounds(map))
			{
				return "OutOfBounds".Translate().CapitalizeFirst();
			}
			if (cell.Fogged(map))
			{
				return "ShuttleCannotLand_Fogged".Translate().CapitalizeFirst();
			}
			if (!cell.Walkable(map))
			{
				return "ShuttleCannotLand_Unwalkable".Translate().CapitalizeFirst();
			}
			RoofDef roof = cell.GetRoof(map);
			if (roof != null && (roof.isNatural || roof.isThickRoof))
			{
				return "MessageTransportPodsDestinationIsInvalid".Translate().CapitalizeFirst();
			}
			List<Thing> thingList = cell.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing is IActiveDropPod || thing is Skyfaller || thing.def.category == ThingCategory.Building)
				{
					return "BlockedBy".Translate(thing).CapitalizeFirst();
				}
				if (!interactionSpot && thing.def.category == ThingCategory.Item)
				{
					return "BlockedBy".Translate(thing).CapitalizeFirst();
				}
				PlantProperties plant = thing.def.plant;
				if (plant != null && plant.IsTree)
				{
					return "BlockedBy".Translate(thing).CapitalizeFirst();
				}
			}
			return null;
		}

		// Token: 0x040031A1 RID: 12705
		private Faction calledFaction;

		// Token: 0x040031A2 RID: 12706
		private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle", true);
	}
}
