class SemanticReleaseError extends Error {
    constructor(message, code, details) {
        super(message);
        Error.captureStackTrace(this, this.constructor);
        this.name = 'SemanticReleaseError';
        this.details = details;
        this.code = code;
        this.semanticRelease = true;
    }
}

module.exports = {
    verifyConditions: [
        () => {
            if (!process.env.GH_TOKEN) {
                throw new SemanticReleaseError(
                    "No GH_TOKEN specified",
                    "ENOGH_TOKEN",
                    "Please make sure to github token in `GH_TOKEN` environment variable on your CI environment. The token must be able to create releases");
            }
            if (!process.env.NUGET_TOKEN) {
                throw new SemanticReleaseError(
                    "No NUGET_TOKEN specified",
                    "ENONUGET_TOKEN",
                    "Please make sure to add NUGET TOKEN in `NUGET_TOKEN` environment variable on your CI environment.");
            }
        },
        "@semantic-release/github"
    ],
    prepare: [
        {
            path: "@semantic-release/exec",
            cmd: "dotnet pack -c Release -p:PackageVersion=${nextRelease.version} --include-source && mkdir out"
        },
        ...[
            "Agoda.Frameworks.DB",
            "Agoda.Frameworks.Grpc",
            "Agoda.Frameworks.Http",
            "Agoda.Frameworks.Http.AutoRestExt",
            "Agoda.Frameworks.LoadBalancing"
        ].map(x => {
            return {
                       path: "@semantic-release/exec",
                       cmd: `cp ./${x}/bin/Release/*.nupkg ./out/`
                   }
        }),
        {
            path: "@semantic-release/exec",
            cmd: "echo nuget publish with source"
        }
    ],
    publish: [
        {
            path: "@semantic-release/exec",
            cmd: `docker push ${serviceName}:\${nextRelease.version}`
        },
        {
            path: "@semantic-release/exec",
            cmd: `docker push ${serviceName}:latest`
        },
        "@semantic-release/github"
    ]
};
