module Client

open WebSharper
open WebSharper.JavaScript

module Highlight =
    open WebSharper.HighlightJS

    [<Require(typeof<Resources.Languages.Fsharp>)>]
    [<Require(typeof<Resources.Styles.Far>)>]
    let Run() =
        JS.Document.QuerySelectorAll("code[class^=language-]").ForEach(
            (fun (node, _, _, _) -> Hljs.HighlightBlock(node)),
            JS.Undefined
        )

module Newsletter =
    let SignUpAction () =
        let button = JS.Document.GetElementById "signup"
        let newsletterForm = JS.Document.GetElementById "newsletter-form"
        if newsletterForm <> null && newsletterForm <> JS.Undefined then
            newsletterForm.AddEventListener("submit", System.Action<Dom.Event>(fun ev ->
                ev.PreventDefault()
            ))
        if button <> null && button <> JS.Undefined then
            button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
                ev.PreventDefault()
                let input = JS.Document.GetElementById "newsletter-input"
                let email : string = input?value
                if email.Trim() <> "" then
                    let alertList = JS.Document.GetElementById "newsletter-alert-list"
                    alertList.ReplaceChildren(([||] : string []))
                    button.SetAttribute("disabled", "disabled")
                    button.ClassList.Add("btn-disabled")
                    let fd = FormData()
                    fd.Append("email", email)
                    fd.Append("type", "Blogs")
                    let options =
                        RequestOptions(
                            Method = "POST",
                            Body = fd
                        )
                    let responsePromise = 
                        JS.Fetch("https://api.intellifactory.com/api/newsletter", options)
                    responsePromise
                        .Then(fun resp ->
                            let successMessage = JS.Document.CreateElement("div")
                            successMessage.ClassName <- "success-alert"
                            successMessage.TextContent <- "You have successfully signed up!"
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
                            alertList.AppendChild successMessage
                        )
                        .Catch(fun _ ->
                            let errorMessage = JS.Document.CreateElement("div")
                            errorMessage.ClassName <- "error-alert"
                            errorMessage.TextContent <- "Sorry, we could not sign you for the newsletter!"
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
                            alertList.AppendChild errorMessage
                        )
                        |> ignore
                else
                    ()
            ))

module Contact =
    let SendMessageAction () =
        let button = JS.Document.GetElementById "contact-button"
        let contactForm = JS.Document.GetElementById "contact-form"
        if contactForm <> null && contactForm <> JS.Undefined then
            contactForm.AddEventListener("submit", System.Action<Dom.Event>(fun ev ->
                ev.PreventDefault()
            ))
        if button <> null && button <> JS.Undefined then
            button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
                ev.PreventDefault()
                let emailInput = JS.Document.QuerySelector "#contact-form *[name=\"email\"]" :?> HTMLInputElement
                let subjectInput = JS.Document.QuerySelector "#contact-form *[name=\"name\"]" :?> HTMLInputElement
                let messageInput = JS.Document.QuerySelector "#contact-form *[name=\"message\"]" :?> HTMLInputElement
                let termsInput = JS.Document.QuerySelector "#contact-form *[name=\"accept_terms\"]" :?> HTMLInputElement
                
                emailInput.ClassList.Remove("input-failed-validation")
                emailInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                subjectInput.ClassList.Remove("input-failed-validation")
                subjectInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                messageInput.ClassList.Remove("input-failed-validation")
                messageInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                termsInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Remove("text-red"))

                let email : string = emailInput.Value
                let subject : string = subjectInput.Value
                let message : string = messageInput.Value
                let terms = termsInput.Checked

                if emailInput.Validity?typeMismatch || email.Trim() = "" then
                    emailInput.ClassList.Add("input-failed-validation")
                    emailInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if subject.Trim() = "" then
                    subjectInput.ClassList.Add("input-failed-validation")
                    subjectInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if message.Trim() = "" then
                    messageInput.ClassList.Add("input-failed-validation")
                    messageInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if not terms then
                    termsInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Add("text-red"))

                if not emailInput.Validity?typeMismatch && subject.Trim() <> "" && message.Trim() <> "" && terms then
                    let alertList = JS.Document.GetElementById "contact-alert-list"
                    alertList.ReplaceChildren(([||] : string []))
                    button.SetAttribute("disabled", "disabled")
                    button.ClassList.Add("btn-disabled")
                    let fd = FormData()
                    fd.Append("email", email)
                    fd.Append("name", subject)
                    fd.Append("message", message)
                    let options =
                        RequestOptions(
                            Method = "POST",
                            Body = fd
                        )
                    let responsePromise = 
                        JS.Fetch("https://api.intellifactory.com/api/contact", options)
                    responsePromise
                        .Then(fun resp ->
                            let modal = JS.Document.QuerySelector "#contact-form .modal"
                            let modalClose = JS.Document.QuerySelector "#contact-form .modal .modal-button"
                            modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                                let modal = JS.Document.QuerySelector "#contact-form .modal"
                                emailInput.Value <- ""
                                subjectInput.Value <- ""
                                messageInput.Value <- ""
                                termsInput.Checked <- false
                                modal.ClassList.Add "hidden"
                                button.RemoveAttribute("disabled")
                                button.ClassList.Remove("btn-disabled")
                            ))
                            modal.ClassList.Remove "hidden"
                        )
                        .Catch(fun _ ->
                            let errorMessage = JS.Document.CreateElement("div")
                            errorMessage.ClassName <- "error-alert"
                            errorMessage.TextContent <- "Sorry, we could not send your message!"
                            button.RemoveAttribute("disabled")
                            button.ClassList.Remove("btn-disabled")
                            alertList.AppendChild errorMessage
                        )
                        |> ignore
                else
                    ()
            ))

module Jobs =
    open WebSharper.UI
    open WebSharper.UI.Client
    open WebSharper.UI.Html

    let totalMax = 10 * 1024 * 1024
    let singleMax = 5 * 1024 * 1024
    let button = JS.Document.GetElementById "JobSend"
    let files : UI.ListModel<string, string * Blob> = UI.ListModel.Create fst []
    let currentSize =
        files.View
        |> UI.View.Map (fun items ->
            items
            |> Seq.map snd
            |> Seq.sumBy (fun x -> x.Size)
        )

    let fileHandle currentSize (fl: FileList) =
        let mutable singleError = None
        let mutable totalError = false
        let mutable fileSize = 0
        for i in [0..fl.Length - 1] do
            let file = fl.Item i
            if singleError = None && file.Size > singleMax then
                singleError <- Some file.Name
            fileSize <- fileSize + file.Size
            
        if singleError = None && currentSize + fileSize > totalMax then
            totalError <- true
        match singleError with
        | None ->
            if totalError then
                let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                let modalClose = JS.Document.QuerySelector "#JobSendFiles .modal .modal-button"
                let modalContent = JS.Document.QuerySelector "#JobSendFiles .modal .modal-content"
                modalContent.TextContent <- "The total file size have exceeded 10MB!"
                modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                    let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                    modal.ClassList.Add "hidden"
                    button.RemoveAttribute("disabled")
                    button.ClassList.Remove("btn-disabled")
                ))
                modal.ClassList.Remove "hidden"
            else
                for i in [0..fl.Length - 1] do
                    let file = fl.Item i
                    files.Add (file.Name, (file :> Blob))
        | Some fileName ->
            let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
            let modalClose = JS.Document.QuerySelector "#JobSendFiles .modal .modal-button"
            let modalContent = JS.Document.QuerySelector "#JobSendFiles .modal .modal-content"
            modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                modalContent.TextContent <- sprintf "%s is larger then 5MB" fileName
                modal.ClassList.Add "hidden"
                button.RemoveAttribute("disabled")
                button.ClassList.Remove("btn-disabled")
            ))
            modal.ClassList.Remove "hidden"

    let SendJobAction () =
        let jobForm = JS.Document.GetElementById "JobSendFiles"
        if jobForm <> null && jobForm <> JS.Undefined then
            files.View.Map(fun items ->
                files.Doc(fun key _ ->
                    div [] [span [attr.``class`` "fileName"] [text key]; span [on.click (fun _ _ -> files.RemoveByKey key)] [text "x"]]
                )
            )
            |> Doc.EmbedView
            |> Doc.RunById "JobFiles"
            jobForm.AddEventListener("submit", System.Action<Dom.Event>(fun ev ->
                ev.PreventDefault()
            ))
            let hiddenInput = JS.Document.QuerySelector "#JobSendFiles #hidden-input"
            let fileUploadButton = JS.Document.QuerySelector "#JobSendFiles #button"
            fileUploadButton.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                hiddenInput?click()
            ))
            hiddenInput.AddEventListener("change", System.Action<Dom.Event>(fun ev ->
                let ev = ev :?> ProgressEvent
                currentSize
                |> UI.View.Get (fun currentSize ->
                    let fl = FileList.OfEvent ev
                    fileHandle currentSize fl
                )
            ))

            let jobUpload = JS.Document.GetElementById "JobUploadArea"
            let dropHandler (ev: Dom.Event) =
                ev.PreventDefault()
                currentSize
                |> UI.View.Get (fun cs ->
                    fileHandle cs ((ev?dataTransfer?files) :> FileList)
                )
                jobUpload.ClassList.Remove "draggedOver"
            let dragLeaveHandler (ev: Dom.Event) =
                ev.PreventDefault()
                jobUpload.ClassList.Remove "draggedOver"
                //if ((ev?dataTransfer?files) :> FileList).Length > 0 then
                //    jobUpload.ClassList.Remove "draggedOver"
            let dragEnterHandler (ev: Dom.Event) =
                ev.PreventDefault()
                jobUpload.ClassList.Add "draggedOver"
            let dragOverHandler (ev: Dom.Event) =
                ev.PreventDefault()
                ev?dataTransfer?dropEffect <- "copy"
                Console.Log(((ev?dataTransfer?files) :> FileList).Length)
            jobUpload.AddEventListener("dragleave", dragLeaveHandler)
            jobUpload.AddEventListener("dragenter", dragEnterHandler)
            jobUpload.AddEventListener("dragover", dragOverHandler)
            jobUpload.AddEventListener("drop", dropHandler)
            ()
            
        if button <> null && button <> JS.Undefined then
            button.AddEventListener("click", System.Action<Dom.Event>(fun ev -> 
                ev.PreventDefault()
                let emailInput = JS.Document.QuerySelector "#JobSendFiles *[name=\"email\"]" :?> HTMLInputElement
                let nameInput = JS.Document.QuerySelector "#JobSendFiles *[name=\"name\"]" :?> HTMLInputElement
                let githubInput = JS.Document.QuerySelector "#JobSendFiles *[name=\"github\"]" :?> HTMLInputElement
                let fileInputError = JS.Document.QuerySelector "#fileUploadError"
        
                emailInput.ClassList.Remove("input-failed-validation")
                emailInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                nameInput.ClassList.Remove("input-failed-validation")
                nameInput.NextElementSibling
                |> Optional.toOption
                |> Option.iter (fun x -> x.ClassList.Add("hidden"))
                fileInputError.ClassList.Remove("hidden")

                let email : string = emailInput.Value
                let name : string = nameInput.Value
                let github : string = githubInput.Value

                if emailInput.Validity?typeMismatch || email.Trim() = "" then
                    emailInput.ClassList.Add("input-failed-validation")
                    emailInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))
                if name.Trim() = "" then
                    nameInput.ClassList.Add("input-failed-validation")
                    nameInput.NextElementSibling
                    |> Optional.toOption
                    |> Option.iter (fun x -> x.ClassList.Remove("hidden"))

                if files.Length = 0 then
                    // Validation for empty files
                    fileInputError.ClassList.Remove("hidden")

                if not emailInput.Validity?typeMismatch && name.Trim() <> "" then
                    button.SetAttribute("disabled", "disabled")
                    button.ClassList.Add("btn-disabled")
                    let fd = FormData()
                    fd.Append("email", email)
                    fd.Append("name", name)
                    if github.Trim() <> "" then
                        fd.Append("github", github)
                    files.Iter(fun (name, data) -> 
                        fd.Append("files", data, name)
                    )
                    let options =
                        RequestOptions(
                            Method = "POST",
                            Body = fd
                        )
                    let responsePromise = 
                        JS.Fetch("https://api.intellifactory.com/api/jobs", options)
                    responsePromise
                        .Then(fun resp ->
                            let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                            let modalClose = JS.Document.QuerySelector "#JobSendFiles .modal .modal-button"
                            modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                                let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                                emailInput.Value <- ""
                                nameInput.Value <- ""
                                githubInput.Value <- ""
                                files.Clear()
                                modal.ClassList.Add "hidden"
                                button.RemoveAttribute("disabled")
                                button.ClassList.Remove("btn-disabled")
                            ))
                            modal.ClassList.Remove "hidden"
                        )
                        .Catch(fun _ ->
                            let modalContentText = "Sorry, we could not send your job application!"
                            let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                            let modalClose = JS.Document.QuerySelector "#JobSendFiles .modal .modal-button"
                            let modalContent = JS.Document.QuerySelector "#JobSendFiles .modal .modal-content"
                            modalContent.TextContent <- modalContentText
                            modalClose.AddEventListener("click", System.Action<Dom.Event>(fun _ ->
                                let modal = JS.Document.QuerySelector "#JobSendFiles .modal"
                                emailInput.Value <- ""
                                nameInput.Value <- ""
                                githubInput.Value <- ""
                                files.Clear()
                                modal.ClassList.Add "hidden"
                                button.RemoveAttribute("disabled")
                                button.ClassList.Remove("btn-disabled")
                            ))
                            modal.ClassList.Remove "hidden"
                        )
                        |> ignore
                else
                    ()
            ))

[<SPAEntryPoint>]
let Main() =
    Highlight.Run()
    Newsletter.SignUpAction()
    Contact.SendMessageAction()
    Jobs.SendJobAction ()

[<assembly:JavaScript>]
do ()
