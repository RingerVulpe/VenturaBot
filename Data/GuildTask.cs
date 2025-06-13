using System;
using System.Collections.Generic;
using System.Linq;

namespace VenturaBot.Data
{
    public enum TaskStatus
    {
        Unapproved,   // right after creation
        Approved,     // after officer approval
        Claimed,      // after a user claims it
        Pending,      // after the claimer marks it complete
        Verified,     // after an officer (or creator) verifies the completed work
        Closed,       // after creator closes a verified task
        Expired,      // if declined or times out
    }

    public class GuildTask
    {
        // ─── Core Fields ──────────────────────────────────────────────────────────
        public int Id { get; set; }
        public TaskType Type { get; set; }
        public int Tier { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; } = "";
        public ulong CreatedBy { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }       // When the modal was submitted

        // ─── Claim / Complete / Verify / Close Fields ───────────────────────────
        public ulong? ClaimedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Verified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? ExpiredAt { get; set; }

        // ─── Tip + Delivery Method ───────────────────────────────────────────────
        public int Tip { get; set; }
        public string DeliveryMethod { get; set; } = "";

        // ─── (Optional) Expiration Deadline ─────────────────────────────────────
        public DateTime? ExpirationDate { get; set; }

        // ─── Community Task Fields ────────────────────────────────────────────────
        /// <summary>
        /// Marks this as a guild-wide community task.
        /// </summary>
        public bool IsCommunityTask { get; set; } = false;
        /// <summary>
        /// Total resource/quantity needed for completion.
        /// </summary>
        public int TotalNeeded { get; set; } = 0;
        /// <summary>
        /// In-game drop-off location description.
        /// </summary>
        public string? DropLocation { get; set; }
        /// <summary>
        /// Total Venturans currency pot to be split among contributors.
        /// </summary>
        public int PotSizeVenturans { get; set; } = 0;
        /// <summary>
        /// Mapping of userId → contributed amount (validated by officers).
        /// </summary>
        public Dictionary<ulong, int> Contributions { get; set; } = new Dictionary<ulong, int>();

        /// <summary>
        /// Sum of all validated contributions.
        /// </summary>
        public int TotalContributed => Contributions.Values.Sum();

        // ─── Type-Specific Fields ────────────────────────────────────────────────
        // --- Gather-specific ---
        public string? Location { get; set; }
        public string? ItemName { get; set; }
        public string? Notes { get; set; }

        // --- Repair-specific ---
        // (Reuse ItemName and Notes; Location serves as repair location)

        // --- CraftingOrder-specific ---
        public string? RecipeName { get; set; }
        public string? MaterialsList { get; set; }
        public string? DeliveryLocation { get; set; }

        // --- VehicleOrder-specific ---
        public string? VehicleModel { get; set; }
        // (Reuse DeliveryLocation to store destination)

        // --- ConstructionOrder-specific ---
        public string? StructureName { get; set; }
        // (Reuse Location to store build location)

        // --- ResourceDelivery-specific ---
        public string? ResourceName { get; set; }
        // (Reuse DeliveryLocation for drop‑off point)

        // --- DeepDesertMap-specific ---
        public int? Sections { get; set; }
        public string? AreaName { get; set; }
        public string? Coordinates { get; set; }

        // --- ExchangeRevenue-specific ---
        public int? Amount { get; set; }
        // (Reuse ItemName for item traded and Location for exchange location)

        // --- SchematicHunt-specific ---
        public string? SchematicName { get; set; }
        // (Reuse Location for search location)

        // --- EventHost-specific ---
        public string? EventName { get; set; }
        public string? EventDateTime { get; set; }
        // (Reuse Location for event location/description)

        // --- GroupExpedition-specific ---
        public string? Objective { get; set; }
        // (Reuse Location for "Start → Destination" string)

        // --- ScoutReport-specific ---
        public string? Observations { get; set; }
        // (Reuse Coordinates for notes)

        /// <summary>
        /// (Optional) The ID of the Discord message on the Task Board
        /// so we can edit it in place instead of reposting.
        /// </summary>
        public ulong? BoardMessageId { get; set; }

        // inside your GuildTask class
        /// <summary>
        /// The Discord channel where this task’s board message was posted.
        /// </summary>
        public ulong ChannelId { get; set; } = 1379731733930446928;

        /// <summary>
        /// If this was created via a recurring-definition, store its ID here.
        /// </summary>
        public string? RecurringDefinitionId { get; set; }
        /// <summary>
        /// Channel where the “other” (original) post was sent
        /// </summary>
        public ulong? OriginChannelId { get; set; }

        /// <summary>
        /// Message ID of that original post
        /// </summary>
        public ulong? OriginMessageId { get; set; }
    }
}
