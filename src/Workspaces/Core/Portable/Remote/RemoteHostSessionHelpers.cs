﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    [Obsolete("Only used by Razor and LUT", error: false)]
    internal sealed class SessionWithSolution : IDisposable
    {
        public readonly RemoteHostClient.Connection Connection;
        private readonly PinnedRemotableDataScope _scope;

        public static async Task<SessionWithSolution> CreateAsync(RemoteHostClient.Connection connection, Solution solution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(connection);
            Contract.ThrowIfNull(solution);

            var service = solution.Workspace.Services.GetRequiredService<IRemotableDataService>();
            var scope = await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);

            SessionWithSolution? session = null;
            try
            {
                // set connection state for this session.
                // we might remove this in future. see https://github.com/dotnet/roslyn/issues/24836
                await connection.InvokeAsync(
                    WellKnownServiceHubServices.ServiceHubServiceBase_Initialize,
                    new object[] { scope.SolutionInfo },
                    cancellationToken).ConfigureAwait(false);

                // transfer ownership of connection and scope to the session object:
                session = new SessionWithSolution(connection, scope);
            }
            finally
            {
                if (session == null)
                {
                    scope.Dispose();
                }
            }

            return session;
        }

        private SessionWithSolution(RemoteHostClient.Connection connection, PinnedRemotableDataScope scope)
        {
            Connection = connection;
            _scope = scope;
        }

        public void Dispose()
        {
            _scope.Dispose();
            Connection.Dispose();
        }
    }
}
