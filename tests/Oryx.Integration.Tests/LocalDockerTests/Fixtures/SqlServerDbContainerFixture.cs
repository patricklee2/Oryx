﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Oryx.Tests.Common;
using Polly;
using Xunit;

namespace Microsoft.Oryx.Integration.Tests.LocalDockerTests.Fixtures
{
    public class SqlServerDbContainerFixture : DbContainerFixtureBase
    {
        private const string DatabaseUsername = "sa";

        public override List<EnvironmentVariable> GetCredentialsAsEnvVars()
        {
            return new List<EnvironmentVariable>
            {
                new EnvironmentVariable(DbServerHostnameEnvVarName, Constants.InternalDbLinkName),
                new EnvironmentVariable(DbServerUsernameEnvVarName, DatabaseUsername),
                new EnvironmentVariable(DbServerPasswordEnvVarName, Constants.DatabaseUserPwd),
                new EnvironmentVariable(DbServerDatabaseEnvVarName, Constants.DatabaseName),
            };
        }

        protected override DockerRunCommandResult RunDbServerContainer()
        {
            var runDbContainerResult = _dockerCli.Run(
                    Settings.MicrosoftSQLServerImageName,
                    environmentVariables: new List<EnvironmentVariable>
                    {
                        new EnvironmentVariable("ACCEPT_EULA", "Y"),
                        new EnvironmentVariable("SA_PASSWORD", Constants.DatabaseUserPwd),
                    },
                    volumes: null,
                    portMapping: null,
                    link: null,
                    runContainerInBackground: true,
                    command: null,
                    commandArguments: null);

            RunAsserts(() => Assert.True(runDbContainerResult.IsSuccess), runDbContainerResult.GetDebugInfo());
            return runDbContainerResult;
        }

        protected override bool WaitUntilDbServerIsUp()
        {
            var retry = Policy.HandleResult(result: false).WaitAndRetry(retryCount: 24, i => TimeSpan.FromSeconds(10));
            return retry.Execute(() =>
            {
                try
                {
                    return _dockerCli.GetContainerLogs(DbServerContainerName)
                            .Contains("SQL Server is now ready for client connections");
                }
                catch
                {
                    // In case of any exception, we consider this retry as a failure and will retry again.
                    return false;
                }
            });
        }

        protected override void InsertSampleData()
        {
            const string sqlFile = "/tmp/sqlserver_setup.sql";
            string baseSqlCmd = $"/opt/mssql-tools/bin/sqlcmd -S localhost -U {DatabaseUsername} -P {Constants.DatabaseUserPwd}";
            var dbSetupScript = new ShellScriptBuilder()
                .CreateFile(sqlFile, GetSampleDataInsertionSql())
                .AddCommand($"{baseSqlCmd} -Q \"CREATE DATABASE {Constants.DatabaseName};\"")
                .AddCommand($"{baseSqlCmd} -i {sqlFile}")
                .ToString();

            var result = _dockerCli.Exec(DbServerContainerName, "/bin/sh", new[] { "-c", dbSetupScript });
            RunAsserts(() => Assert.True(result.IsSuccess), result.GetDebugInfo());
        }

        protected override void StopContainer()
        {
            // We have noticed that 'docker stop' of the sql server container does not work always,
            // So instead kill the process from within the container itself.
            var result = _dockerCli.Exec(
                DbServerContainerName,
                "/bin/sh",
                //NOTE: sqlservr is not a typo! It is the name of the process
                new[] { "-c", "kill -SIGKILL $(pgrep sqlservr)" });

            // Call to base to stop the container itself if the above step did not stop it.
            base.StopContainer();
        }
    }
}
