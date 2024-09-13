// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

namespace DotnetCore.QueueSlim.Internal
{
    /// <summary>
    /// Used to verify cap message queue extension was added on a ServiceCollection
    /// </summary>
    public class QueueMarkerService
    {
        public string Name { get; set; }

        public QueueMarkerService(string name)
        {
            Name = name;
        }
    }
}
