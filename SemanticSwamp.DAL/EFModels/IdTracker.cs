using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

/// <summary>
/// A single-row table used to track the last vector ID issued to Qdrant.
///
/// Qdrant uses ulong point IDs. Rather than querying Qdrant to find the next available ID
/// (which would require an extra round-trip), this table acts as a simple monotonically
/// increasing counter. After each batch of vectors is upserted, RAGManager increments
/// LastIdUsed by the number of chunks written and persists the new value here.
///
/// The table is expected to always contain exactly one row.
/// </summary>
public partial class IdTracker
{
    /// <summary>
    /// The highest Qdrant point ID that has been assigned so far. The next upsert batch
    /// starts numbering from LastIdUsed + 1 (or from LastIdUsed cast to ulong directly,
    /// as RAGManager increments it post-assignment).
    /// </summary>
    public int LastIdUsed { get; set; }

    /// <summary>Primary key of this tracking row (always 1 in practice).</summary>
    public int Id { get; set; }
}
