// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace DotnetCore.QueueSlim.Options
{
    public class QueueSlimOptions
    {
        public bool RetryOnAsyncCallback { get; set; } = true;

        public IList<TimeSpan> AsyncCallbackRetryIntervals { get; set; } = new List<TimeSpan>
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(15),
        };

        internal IList<IQueueOptionsExtension> Extensions { get; }
        public QueueSlimOptions()
        {
            Extensions = new List<IQueueOptionsExtension>();
        }

        public void RegisterExtension(IQueueOptionsExtension extension)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));

            Extensions.Add(extension);
        }
    }

    public interface IQueueOptionsExtension
    {
        void AddServices(IServiceCollection services);
    }
}
