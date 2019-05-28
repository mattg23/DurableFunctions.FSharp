﻿namespace DurableFunctions.FSharp

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open OrchestratorBuilder

type Orchestrator = class

    /// Runs a workflow which expects an input parameter by reading this parameter from 
    /// the orchestration context.
    static member run (workflow : ContextTask<'b>, context : IDurableOrchestrationContext) : Task<'b> = 
        workflow context

    /// Runs a workflow which expects an input parameter by reading this parameter from 
    /// the orchestration context.
    static member run (workflow : 'a -> ContextTask<'b>, context : IDurableOrchestrationContext) : Task<'b> = 
        let input = context.GetInput<'a> ()
        workflow input context
    
    /// Returns a fixed value as a orchestrator.
    static member ret value (_: IDurableOrchestrationContext) =
        Task.FromResult value

    /// Delays orchestrator execution by the specified timespan.
    static member delay (timespan: TimeSpan) (context: IDurableOrchestrationContext) =
        let deadline = context.CurrentUtcDateTime.Add timespan
        context.CreateTimer(deadline, CancellationToken.None)
    
    /// Wait for an external event. maxTimeToWait specifies the longest period to wait:
    /// the call will return an Error if timeout is reached.
    static member waitForEvent<'a> (maxTimeToWait: TimeSpan) (eventName: string) (context: IDurableOrchestrationContext) =
        let deadline = context.CurrentUtcDateTime.Add maxTimeToWait
        let timer = context.CreateTimer(deadline, CancellationToken.None)
        let event = context.WaitForExternalEvent<'a> eventName
        Task.WhenAny(event, timer)
            .ContinueWith(
                fun (winner: Task) -> 
                    if winner = timer then Result.Error ""
                    else Result.Ok event.Result)
end