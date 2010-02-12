module Calculator

type Operator =
    | Reset
    | Get of AsyncReplyChannel<float>
    | Add of float
    | Subtract of float
    | Multiply of float
    | Divide of float

type Calculator() =
    let processor = MailboxProcessor.Start(fun inbox ->
        let rec loop cur =
            async {
                let! op = inbox.Receive()
                match op with
                | Reset -> return! loop 0.0
                | Get reply ->
                    reply.Reply(cur)
                    return! loop cur
                | Add n -> return! loop (cur+n)
                | Subtract n -> return! loop (cur-n)
                | Multiply n -> return! loop (cur*n)
                | Divide n -> return! loop (cur/n)
            }
        loop 0.0
    )

    member x.Processor = processor
