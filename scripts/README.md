# NuGet Publishing Scripts

This directory contains scripts for automating the version bumping and NuGet publishing process for TiMoch.Orleans.TestUtilities.

## Scripts Overview

### 📦 `bump-version.sh`
Automated version bumping with semantic versioning support.

**Features:**
- Semantic versioning (patch/minor/major bumps)
- Automatic changelog updates
- Git commit and tag creation
- Dry-run mode for testing
- Backup creation for safety

**Usage:**
```bash
# Bump patch version (1.0.0 -> 1.0.1)
./scripts/bump-version.sh patch

# Bump minor version (1.0.0 -> 1.1.0)  
./scripts/bump-version.sh minor

# Bump major version (1.0.0 -> 2.0.0)
./scripts/bump-version.sh major

# Test changes without applying them
./scripts/bump-version.sh patch --dry-run

# Alternative syntax
./scripts/bump-version.sh -t minor --dry-run
```

### 🚀 `publish-nuget.sh`
Automated NuGet package building and publishing.

**Features:**
- Package building and validation
- NuGet.org publishing
- Existing version detection
- Dry-run mode for testing
- Interactive and automated modes

**Usage:**
```bash
# Test package building without publishing
./scripts/publish-nuget.sh --dry-run

# Publish with API key
./scripts/publish-nuget.sh --api-key YOUR_NUGET_API_KEY

# Interactive mode (will prompt for API key)
./scripts/publish-nuget.sh

# Use existing package without rebuilding
./scripts/publish-nuget.sh --skip-build --dry-run
```

**Environment Variables:**
```bash
export NUGET_API_KEY="your-api-key-here"
./scripts/publish-nuget.sh
```

## GitHub Actions Workflow

### 🔄 `.github/workflows/nuget-publish.yml`
Comprehensive CI/CD pipeline for automated testing and publishing.

**Triggers:**
- **Push to main/develop**: Build and test
- **Version tags (v*)**: Publish to NuGet.org
- **Pull requests**: Validate changes
- **Manual dispatch**: On-demand publishing

**Jobs:**
1. **Build and Test** - Compile, test, and create package
2. **Validate Package** - Verify package integrity and contents
3. **Security Scan** - CodeQL analysis and vulnerability scanning
4. **Publish Preview** - Publish to GitHub Packages (develop branch)
5. **Publish Release** - Publish to NuGet.org (version tags)

## Complete Release Process

### 1. Development Workflow
```bash
# Make your changes
git checkout -b feature/my-feature
# ... make changes ...
git commit -m "feat: add new feature"
git push origin feature/my-feature
```

### 2. Version Bump and Release
```bash
# Test version bump first
./scripts/bump-version.sh patch --dry-run

# Apply version bump (creates commit and tag)
./scripts/bump-version.sh patch

# Push changes and tags
git push && git push --tags
```

### 3. Automated Publishing
The GitHub Actions workflow will automatically:
- Build and test the package
- Validate package integrity
- Run security scans
- Publish to NuGet.org (triggered by version tag)
- Create GitHub release with package attachment

### 4. Manual Publishing (if needed)
```bash
# Build and test locally
./scripts/publish-nuget.sh --dry-run

# Publish to NuGet.org
./scripts/publish-nuget.sh --api-key $NUGET_API_KEY
```

## Configuration

### Required Secrets (GitHub Actions)
Set these in your GitHub repository settings:

- `NUGET_API_KEY`: Your NuGet.org API key for publishing packages

### Project Requirements
- .NET 8.0 SDK or later
- NuGet CLI tools
- Git repository with proper configuration

### File Structure
```
├── scripts/
│   ├── bump-version.sh          # Version bumping
│   ├── publish-nuget.sh         # NuGet publishing  
│   └── README.md               # This file
├── .github/workflows/
│   └── nuget-publish.yml       # CI/CD pipeline
├── src/TiMoch.Orleans.TestUtilities/
│   ├── TiMoch.Orleans.TestUtilities.csproj  # Package metadata
│   ├── CHANGELOG.md            # Release notes
│   └── README.md               # Package documentation
└── artifacts/nuget/            # Build output directory
```

## Troubleshooting

### Common Issues

**Script Permission Errors:**
```bash
chmod +x scripts/*.sh
```

**Line Ending Issues (Windows):**
```bash
dos2unix scripts/*.sh
```

**Missing .NET SDK:**
```bash
# Install .NET SDK 8.0+
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
```

**Package Build Failures:**
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Script Debugging
Both scripts support verbose output and dry-run modes:

```bash
# Test version bump
./scripts/bump-version.sh patch --dry-run

# Test package build  
./scripts/publish-nuget.sh --dry-run

# Check prerequisites
./scripts/publish-nuget.sh --help
```

### API Key Management
For security, use environment variables or GitHub secrets:

```bash
# Local development
export NUGET_API_KEY="oy2..."

# GitHub Actions (already configured)
# Set NUGET_API_KEY in repository secrets
```

## Best Practices

### Version Bumping
- Use **patch** for bug fixes and small improvements
- Use **minor** for new features (backward compatible)
- Use **major** for breaking changes
- Always test with `--dry-run` first
- Review changes before pushing tags

### Package Publishing
- Always validate packages with dry-run mode
- Use meaningful commit messages following conventional commits
- Tag releases with descriptive release notes
- Monitor NuGet.org for package availability
- Test package installation in separate projects

### CI/CD Pipeline
- Review all security scan results
- Monitor build logs for warnings
- Ensure all tests pass before publishing  
- Use GitHub environments for production releases
- Set up branch protection rules for main/develop

## Support

For issues with these scripts or the publishing process:

1. Check the troubleshooting section above
2. Review script output for error details
3. Test with dry-run mode to identify issues
4. Check GitHub Actions logs for CI/CD problems
5. Verify NuGet API key permissions and validity

## Security Notes

- **Never commit API keys** to the repository
- Use GitHub secrets for sensitive configuration
- Enable security scanning in the CI/CD pipeline
- Monitor for vulnerability alerts on dependencies
- Use least-privilege API keys from NuGet.org