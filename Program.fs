// Learn more about F# at http://fsharp.org

open System
open RestSharp
open RestSharp.Authenticators

type AccessToken =
    | AccessToken of string
    with
        override this.ToString() =
            match this with | AccessToken t -> t

[<CLIMutable>]
type TokenResponse = {
    AccessToken: string
    ExpiresIn: int64
    TokenType: string
}

let handleResponse<'response> (value: IRestResponse<'response>): 'response =
    if not value.IsSuccessful then
        failwithf "Failed: %s - %s" value.Content value.ErrorMessage
    else
        value.Data

let getToken (client: RestClient) (userName: string) (password: string) =
    let request =
        RestRequest("token", Method.POST)
            .AddParameter("grant_type", "password")
            .AddParameter("username", userName)
            .AddParameter("password", password)

    let response = client.Execute<TokenResponse>(request) |> handleResponse
    AccessToken (response.AccessToken)

[<CLIMutable>]
type DocumentInfo = {
    DocumentId: int64
    EmployeeId: int64
    DocumentTypeId: int32
    DateCreated: string
    ServiceItemId: int64
    PeriodYear: string
    PeriodDescription: string
    GroupDescription: string
    DocumentTypeDescription: string
}

let getDocuments (client: RestClient) =
    let request = RestRequest("employee/documents/GetDocuments")
    let response = client.Execute<ResizeArray<DocumentInfo>>(request) |> handleResponse
    response |> List.ofSeq

[<CLIMutable>]
type DocumentFile = {
    FileName: string
    FileBuffer: string
}

let downloadDocument (client: RestClient) (id: int64) =
    let request =
        RestRequest("employee/documents/DownloadDocuments")
            .AddQueryParameter("documentIds", id.ToString())

    let response = client.Execute<DocumentFile>(request) |> handleResponse

    let bytes = Convert.FromBase64String(response.FileBuffer)

    response.FileName, bytes

[<EntryPoint>]
let main argv =
    let client = RestClient "https://portal.immedis.com/api/"
    
    let accessToken = getToken client (argv.[0]) (argv.[1])
    printfn "%A" accessToken
    client.Authenticator <- new JwtAuthenticator (accessToken.ToString())

    let docs = getDocuments client
    printfn "%A" docs

    for doc in docs do
        let (name, bytes) = downloadDocument client doc.DocumentId
        System.IO.File.WriteAllBytes(name, bytes)

    0 // return an integer exit code
