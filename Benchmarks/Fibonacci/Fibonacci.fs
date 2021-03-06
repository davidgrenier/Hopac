﻿// Copyright (C) by Housemarque, Inc.

module Fibonacci

/////////////////////////////////////////////////////////////////////////

open Hopac
open Hopac.Job.Infixes
open System
open System.Threading
open System.Threading.Tasks
open System.Diagnostics

/////////////////////////////////////////////////////////////////////////

let mutable numSpawns = 0L

module SerialFun =
  let rec fib n =
    if n < 2L then
      n
    else
      numSpawns <- numSpawns + 1L
      fib (n-2L) + fib (n-1L)

  let run n =
    numSpawns <- 0L
    printf "SerFun: "
    let timer = Stopwatch.StartNew ()
    let r = fib n
    let d = timer.Elapsed
    printf "%d - %fs (%d recs)\n" r d.TotalSeconds numSpawns

/////////////////////////////////////////////////////////////////////////

module SerialJob =
  let rec fib n = job {
    if n < 2L then
      return n
    else
      let! x = fib (n-2L)
      let! y = fib (n-1L)
      return x + y
  }

  let run n =
    printf "SerJob: "
    let timer = Stopwatch.StartNew ()
    let r = run (fib n)
    let d = timer.Elapsed
    printf "%d - %fs\n" r d.TotalSeconds

/////////////////////////////////////////////////////////////////////////

module SerialOpt =
  let rec fib n =
    if n < 2L then
      Job.result n
    else
      fib (n-2L) >>= fun x ->
      fib (n-1L) |>> fun y ->
      x + y

  let run n =
    printf "SerOpt: "
    let timer = Stopwatch.StartNew ()
    let r = run (fib n)
    let d = timer.Elapsed
    printf "%d - %fs\n" r d.TotalSeconds

/////////////////////////////////////////////////////////////////////////

module ParallelJob =
  let rec fib n = job {
    if n < 2L then
      return n
    else
      let! (x, y) = fib (n-2L) <*> fib (n-1L)
      return x + y
  }

  let run n =
    printf "ParJob: "
    let timer = Stopwatch.StartNew ()
    let r = run (fib n)
    let d = timer.Elapsed
    printf "%d - %fs (%f jobs/s)\n"
     r d.TotalSeconds (float numSpawns / d.TotalSeconds)

/////////////////////////////////////////////////////////////////////////

module ParallelOpt =
  let rec fib n =
    if n < 2L then
      Job.result n
    else
      Job.delay <| fun () ->
      fib (n-2L) <*> fib (n-1L) |>> fun (x, y) ->
      x + y

  let run n =
    printf "ParOpt: "
    let timer = Stopwatch.StartNew ()
    let r = run (fib n)
    let d = timer.Elapsed
    printf "%d - %fs (%f jobs/s)\n"
     r d.TotalSeconds (float numSpawns / d.TotalSeconds)

/////////////////////////////////////////////////////////////////////////

module SerAsc =
  let rec fib n = async {
    if n < 2L then
      return n
    else
      let! x = fib (n-2L)
      let! y = fib (n-1L)
      return x + y
  }

  let run n =
    printf "SerAsc: "
    let timer = Stopwatch.StartNew ()
    let r = Async.RunSynchronously (fib n)
    let d = timer.Elapsed
    printf "%d - %fs\n" r d.TotalSeconds

/////////////////////////////////////////////////////////////////////////

module ParAsc =
  let rec fib n = async {
    if n < 2L then
      return n
    else
      let! x = Async.StartChild (fib (n-2L))
      let! y = fib (n-1L)
      let! x = x
      return x + y
  }

  let run n =
    printf "ParAsc: "
    let timer = Stopwatch.StartNew ()
    let r = Async.RunSynchronously (fib n)
    let d = timer.Elapsed
    printf "%d - %fs\n" r d.TotalSeconds

/////////////////////////////////////////////////////////////////////////

module Task =
  let rec fib n =
    if n < 2L then
      n
    else
      let x = Task.Factory.StartNew (fun _ -> fib (n-2L))
      let y = fib (n-1L)
      x.Result + y

  let run n =
    printf "ParTsk: "
    let timer = Stopwatch.StartNew ()
    let r = fib n
    let d = timer.Elapsed
    printf "%d - %fs (%f tasks/s)\n"
     r d.TotalSeconds (float numSpawns / d.TotalSeconds)

/////////////////////////////////////////////////////////////////////////

let cleanup () =
  for i=1 to 5 do
    Runtime.GCSettings.LargeObjectHeapCompactionMode <- Runtime.GCLargeObjectHeapCompactionMode.CompactOnce
    GC.Collect ()
    Threading.Thread.Sleep 50

do for n in [10L; 20L; 30L; 40L] do
     SerialFun.run n ; cleanup ()
     ParallelOpt.run n ; cleanup ()
     ParallelJob.run n ; cleanup ()
     SerialOpt.run n ; cleanup ()
     SerialJob.run n ; cleanup ()
     SerAsc.run n ; cleanup ()
     Task.run n ; cleanup ()
     if n <= 30L then
       ParAsc.run n ; cleanup ()
