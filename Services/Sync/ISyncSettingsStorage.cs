using System;

namespace ResQLink.Services.Sync;

public interface ISyncSettingsStorage
{
    void LoadInto(SyncSettings target);
    void Save(SyncSettings source);
}
