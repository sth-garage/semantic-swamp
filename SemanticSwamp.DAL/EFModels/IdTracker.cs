using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

public partial class IdTracker
{
    public int LastIdUsed { get; set; }

    public int Id { get; set; }
}
