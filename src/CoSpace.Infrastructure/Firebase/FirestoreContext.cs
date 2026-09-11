using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace CoSpace.Infrastructure.Firebase;

public sealed class FirestoreContext
{
    public FirestoreDb Db { get; }

    public FirestoreContext(IConfiguration configuration)
    {
        var projectId = configuration["FIREBASE_PROJECT_ID"] ?? throw new InvalidOperationException("FIREBASE_PROJECT_ID no está configurado.");
        var json = configuration["FIREBASE_SERVICE_ACCOUNT_JSON"] ?? throw new InvalidOperationException("FIREBASE_SERVICE_ACCOUNT_JSON no está configurado.");
        Db = new FirestoreDbBuilder { ProjectId = projectId, Credential = GoogleCredential.FromJson(json) }.Build();
    }
}