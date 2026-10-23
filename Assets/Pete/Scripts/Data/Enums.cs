namespace Pete.Data
{
    public enum GhostType
    {
        None = 0,
        Normal = 1,
        Threat = 2, // ผีขู่ ("จะมาหลอกหลอน")
        Scream = 3  // ผีกรี๊ด
    }

    public enum CallType
    {
        JobOffer = 0,   // งานจริง
        Scam = 1,       // มิจฉาชีพ
        Family = 2,     // ครอบครัว/คนรู้จัก
        GhostThreat = 3,// ผีพูดขู่
        GhostScream = 4 // ผีเสียงกรี๊ด
    }

    public enum EndingType
    {
        GoodEnd = 0,            // ได้งานทำ
        BadEndScammed = 1,      // โดนหลอกหมดตัว
        BadEndUnemployed = 2    // ตกงานเพราะไม่ยอมรับงาน
    }

    public enum InspectTargetType
    {
        None = 0,
        Computer = 1,
        Phone = 2,
        Note = 3,
        Shrine = 4
    }
}
