﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wesley.Client.Services
{
    public interface ISettingService
    {
        void GetCompanySettingAsync(CancellationToken calToken = default);
        Task<Dictionary<int, string>> GetRemarkConfigListSetting(CancellationToken calToken = default);
    }
}