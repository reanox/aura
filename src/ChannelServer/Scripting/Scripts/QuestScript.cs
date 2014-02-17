﻿// Copyright (c) Aura development team - Licensed under GNU GPL
// For more information, see license file in the main folder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Channel.World.Quests;
using System.Collections;
using Aura.Shared.Mabi.Const;
using Aura.Shared.Util;
using Aura.Channel.World.Entities;
using Aura.Shared.Mabi;
using Aura.Channel.Network.Sending;
using System.Threading;

namespace Aura.Channel.Scripting.Scripts
{
	public class QuestScript : BaseScript
	{
		public int Id { get; set; }

		public string Name { get; set; }
		public string Description { get; set; }

		public Receive ReceiveMethod { get; set; }
		public bool Cancelable { get; set; }

		public List<QuestPrerequisite> Prerequisites { get; protected set; }
		public OrderedDictionary<string, QuestObjective> Objectives { get; protected set; }
		public List<QuestReward> Rewards { get; protected set; }

		/// <summary>
		/// Used in quest items, although seemingly not required.
		/// </summary>
		public MabiDictionary MetaData { get; protected set; }

		public QuestScript()
		{
			this.Prerequisites = new List<QuestPrerequisite>();
			this.Objectives = new OrderedDictionary<string, QuestObjective>();
			this.Rewards = new List<QuestReward>();

			this.MetaData = new MabiDictionary();
		}

		public override void Dispose()
		{
			base.Dispose();

			ChannelServer.Instance.Events.PlayerLoggedIn -= this.OnPlayerLoggedIn;
			ChannelServer.Instance.Events.CreatureKilledByPlayer -= this.OnCreatureKilledByPlayer;
			ChannelServer.Instance.Events.PlayerReceivesItem -= this.OnPlayerReceivesOrRemovesItem;
			ChannelServer.Instance.Events.PlayerRemovesItem -= this.OnPlayerReceivesOrRemovesItem;
		}

		// Setup
		// ------------------------------------------------------------------

		/// <summary>
		/// Sets id of quest.
		/// </summary>
		/// <param name="id"></param>
		protected void SetId(int id)
		{
			this.Id = id;
		}

		/// <summary>
		/// Sets name of quest.
		/// </summary>
		/// <param name="name"></param>
		protected void SetName(string name)
		{
			this.Name = name;
		}

		/// <summary>
		/// Sets description of quest.
		/// </summary>
		/// <param name="description"></param>
		protected void SetDescription(string description)
		{
			this.Description = description;
		}

		/// <summary>
		/// Sets the way you receive the quest.
		/// </summary>
		/// <param name="method"></param>
		protected void SetReceive(Receive method)
		{
			this.ReceiveMethod = method;
		}

		/// <summary>
		/// Adds prerequisite that has to be met before auto receiving the quest.
		/// </summary>
		/// <param name="prerequisite"></param>
		protected void AddPrerequisite(QuestPrerequisite prerequisite)
		{
			this.Prerequisites.Add(prerequisite);
		}

		/// <summary>
		/// Adds objective that has to be cleared to complete the quest.
		/// </summary>
		/// <param name="ident"></param>
		/// <param name="description"></param>
		/// <param name="regionId"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="objective"></param>
		protected void AddObjective(string ident, string description, int regionId, int x, int y, QuestObjective objective)
		{
			if (this.Objectives.ContainsKey(ident))
			{
				Log.Error("{0}: Objectives must have an unique identifier.", this.GetType().Name);
				return;
			}

			objective.Ident = ident;
			objective.Description = description;
			objective.RegionId = regionId;
			objective.X = x;
			objective.Y = y;

			if (objective.Type == ObjectiveType.Kill)
			{
				ChannelServer.Instance.Events.CreatureKilledByPlayer -= this.OnCreatureKilledByPlayer;
				ChannelServer.Instance.Events.CreatureKilledByPlayer += this.OnCreatureKilledByPlayer;
			}

			if (objective.Type == ObjectiveType.Collect)
			{
				ChannelServer.Instance.Events.PlayerReceivesItem -= this.OnPlayerReceivesOrRemovesItem;
				ChannelServer.Instance.Events.PlayerReceivesItem += this.OnPlayerReceivesOrRemovesItem;
				ChannelServer.Instance.Events.PlayerRemovesItem -= this.OnPlayerReceivesOrRemovesItem;
				ChannelServer.Instance.Events.PlayerRemovesItem += this.OnPlayerReceivesOrRemovesItem;
			}

			this.Objectives.Add(ident, objective);
		}

		protected void AddReward(QuestReward reward)
		{
			this.Rewards.Add(reward);
		}

		protected void AddHook(string npc, string hook, Func<IEnumerable> func)
		{

		}

		// Prerequisite Factory
		// ------------------------------------------------------------------

		protected QuestPrerequisite Completed(int questId) { return new QuestPrerequisiteQuestCompleted(questId); }
		protected QuestPrerequisite ReachedLevel(int level) { return new QuestPrerequisiteReachedLevel(level); }
		protected QuestPrerequisite And(params QuestPrerequisite[] prerequisites) { return new QuestPrerequisiteAnd(prerequisites); }
		protected QuestPrerequisite Or(params QuestPrerequisite[] prerequisites) { return new QuestPrerequisiteOr(prerequisites); }

		// Objective Factory
		// ------------------------------------------------------------------

		protected QuestObjective Kill(int amount, string raceType) { return new QuestObjectiveKill(amount, raceType); }
		protected QuestObjective Collect(int itemId, int amount) { return new QuestObjectiveCollect(itemId, amount); }
		protected QuestObjective Talk(string npcName) { return new QuestObjectiveTalk(npcName); }

		// Reward Factory
		// ------------------------------------------------------------------

		protected QuestReward Item(int itemId, int amount) { return new QuestRewardItem(itemId, amount); }
		protected QuestReward Skill(SkillId skillId, SkillRank rank) { return new QuestRewardSkill(skillId, rank); }
		protected QuestReward Gold(int amount) { return new QuestRewardGold(amount); }
		protected QuestReward Exp(int amount) { return new QuestRewardExp(amount); }
		protected QuestReward ExplExp(int amount) { return new QuestRewardExplExp(amount); }
		protected QuestReward AP(short amount) { return new QuestRewardAp(amount); }

		// Where the magic happens~
		// ------------------------------------------------------------------

		/// <summary>
		/// Sets up necessary subscriptions.
		/// </summary>
		public void Init()
		{
			if (this.ReceiveMethod == Receive.Auto)
				ChannelServer.Instance.Events.PlayerLoggedIn += this.OnPlayerLoggedIn;

			this.MetaData.SetString("QSTTIP", "N_{0}|D_{1}|A_|R_{2}|T_0", this.Name, this.Description, string.Join(", ", this.Rewards));
		}

		/// <summary>
		/// Checks and starts auto quests.
		/// </summary>
		/// <param name="character"></param>
		private void OnPlayerLoggedIn(PlayerCreature character)
		{
			this.CheckPrerequisites(character);
		}

		/// <summary>
		/// Starts quest if prerequisites are met.
		/// </summary>
		/// <param name="character"></param>
		/// <returns></returns>
		private bool CheckPrerequisites(PlayerCreature character)
		{
			if (this.ReceiveMethod != Receive.Auto || character.Quests.Has(this.Id))
				return false;

			foreach (var prerequisite in this.Prerequisites)
			{
				if (!prerequisite.Met(character))
					return false;
			}

			character.Quests.Start(this.Id);

			return true;
		}

		/// <summary>
		/// Updates kill objectives.
		/// </summary>
		/// <param name="creature"></param>
		/// <param name="character"></param>
		private void OnCreatureKilledByPlayer(Creature creature, PlayerCreature character)
		{
			if (creature == null || character == null) return;

			var quest = character.Quests.Get(this.Id);
			if (quest == null) return;

			var progress = quest.CurrentObjective;
			if (progress == null) return;

			var objective = this.Objectives[progress.Ident] as QuestObjectiveKill;
			if (objective == null || objective.Type != ObjectiveType.Kill || !objective.Check(creature)) return;

			if (progress.Count >= objective.Amount) return;

			progress.Count++;

			if (progress.Count >= objective.Amount)
				quest.SetDone(progress.Ident);

			Send.QuestUpdate(character, quest);
		}

		/// <summary>
		/// Updates collect objectives.
		/// </summary>
		/// <param name="creatue"></param>
		/// <param name="itemId"></param>
		/// <param name="amount"></param>
		private void OnPlayerReceivesOrRemovesItem(PlayerCreature character, int itemId, int amount)
		{
			if (character == null) return;

			var quest = character.Quests.Get(this.Id);
			if (quest == null) return;

			var progress = quest.CurrentObjectiveOrLast;
			if (progress == null) return;

			var objective = this.Objectives[progress.Ident] as QuestObjectiveCollect;
			if (objective == null || objective.Type != ObjectiveType.Collect || itemId != objective.ItemId) return;

			if (progress.Count >= objective.Amount) return;

			progress.Count = character.Inventory.Count(itemId);

			if (!progress.Done && progress.Count >= objective.Amount)
				quest.SetDone(progress.Ident);
			else if (progress.Done && progress.Count < objective.Amount)
				quest.SetUndone(progress.Ident);

			Send.QuestUpdate(character, quest);
		}
	}

	public enum Receive
	{
		Manually,
		Auto,
	}
}
