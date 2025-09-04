#!/bin/bash

# Version bumping script for TiMoch.Orleans.TestUtilities
# Usage: ./scripts/bump-version.sh [patch|minor|major] [--dry-run]
# Alternative: ./scripts/bump-version.sh -t [patch|minor|major] [--dry-run]

set -e

# Configuration
PROJECT_FILE="src/TiMoch.Orleans.TestUtilities/TiMoch.Orleans.TestUtilities.csproj"
CHANGELOG_FILE="src/TiMoch.Orleans.TestUtilities/CHANGELOG.md"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

show_usage() {
    cat << EOF
Usage: $0 [patch|minor|major] [--dry-run]
   or: $0 -t [patch|minor|major] [--dry-run]

Bump version for TiMoch.Orleans.TestUtilities NuGet package.

Arguments:
    patch       Increment patch version (1.0.0 -> 1.0.1)
    minor       Increment minor version (1.0.0 -> 1.1.0)
    major       Increment major version (1.0.0 -> 2.0.0)

Options:
    -t          Type flag (alternative syntax)
    --dry-run   Show what would be changed without making changes

Examples:
    $0 patch            # 1.0.0 -> 1.0.1
    $0 minor            # 1.0.0 -> 1.1.0
    $0 major            # 1.0.0 -> 2.0.0
    $0 -t patch --dry-run   # Show changes without applying
EOF
}

get_current_version() {
    if [ ! -f "$PROJECT_FILE" ]; then
        log_error "Project file not found: $PROJECT_FILE"
        exit 1
    fi
    
    # Extract version from csproj file
    local version=$(grep -oP '<Version>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || echo "")
    
    if [ -z "$version" ]; then
        # Try PackageVersion if Version is not found
        version=$(grep -oP '<PackageVersion>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || echo "")
    fi
    
    if [ -z "$version" ]; then
        log_error "Could not find version in $PROJECT_FILE"
        log_info "Looking for <Version> or <PackageVersion> tags"
        exit 1
    fi
    
    echo "$version"
}

validate_version() {
    local version="$1"
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
        log_error "Invalid version format: $version (expected: major.minor.patch)"
        exit 1
    fi
}

bump_version() {
    local current_version="$1"
    local bump_type="$2"
    
    validate_version "$current_version"
    
    IFS='.' read -r major minor patch <<< "$current_version"
    
    case "$bump_type" in
        patch)
            patch=$((patch + 1))
            ;;
        minor)
            minor=$((minor + 1))
            patch=0
            ;;
        major)
            major=$((major + 1))
            minor=0
            patch=0
            ;;
        *)
            log_error "Invalid bump type: $bump_type"
            show_usage
            exit 1
            ;;
    esac
    
    echo "${major}.${minor}.${patch}"
}

update_project_file() {
    local new_version="$1"
    local dry_run="$2"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would update $PROJECT_FILE with version $new_version"
        log_info "Changes that would be made:"
        sed "s/<Version>[^<]*<\/Version>/<Version>$new_version<\/Version>/g; s/<PackageVersion>[^<]*<\/PackageVersion>/<PackageVersion>$new_version<\/PackageVersion>/g" "$PROJECT_FILE" | diff "$PROJECT_FILE" - || true
        return
    fi
    
    
    # Update version tags
    sed -i "s/<Version>[^<]*<\/Version>/<Version>$new_version<\/Version>/g; s/<PackageVersion>[^<]*<\/PackageVersion>/<PackageVersion>$new_version<\/PackageVersion>/g" "$PROJECT_FILE"
    
    log_success "Updated $PROJECT_FILE with version $new_version"
}

update_changelog() {
    local new_version="$1"
    local dry_run="$2"
    local date=$(date "+%Y-%m-%d")
    
    if [ ! -f "$CHANGELOG_FILE" ]; then
        if [ "$dry_run" = "true" ]; then
            log_info "Would create $CHANGELOG_FILE"
            return
        fi
        
        cat > "$CHANGELOG_FILE" << EOF
# Changelog

All notable changes to TiMoch.Orleans.TestUtilities will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [$new_version] - $date

### Added
- Initial release of TiMoch.Orleans.TestUtilities
- OrleansTestClusterFixture for test cluster management
- OrleansTestBase with polling utilities
- NUnitTestContextLoggerProvider for test logging
- FluentTestSiloConfigurator for easy setup
EOF
        log_success "Created $CHANGELOG_FILE"
        return
    fi
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would update $CHANGELOG_FILE with version $new_version"
        return
    fi
    
    
    # Add new version entry after [Unreleased]
    sed -i "/## \[Unreleased\]/a\\
\\
## [$new_version] - $date\\
\\
### Changed\\
- Version bump to $new_version" "$CHANGELOG_FILE"
    
    log_success "Updated $CHANGELOG_FILE with version $new_version"
}

commit_changes() {
    local new_version="$1"
    local dry_run="$2"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would commit changes and create tag v$new_version"
        return
    fi
    
    # Check if we're in a git repository
    if ! git rev-parse --git-dir > /dev/null 2>&1; then
        log_warning "Not in a git repository. Skipping commit and tag creation."
        return
    fi
    
    # Add changes to git
    git add "$PROJECT_FILE" "$CHANGELOG_FILE" 2>/dev/null || true
    
    # Check if there are changes to commit
    if git diff --cached --quiet; then
        log_warning "No changes to commit"
        return
    fi
    
    # Commit changes
    git commit -m "chore(release): bump version to $new_version

- Update TiMoch.Orleans.TestUtilities to version $new_version
- Update changelog with release notes

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>"
    
    # Create annotated tag
    git tag -a "v$new_version" -m "Release version $new_version"
    
    log_success "Created commit and tag v$new_version"
    log_info "To push changes: git push && git push --tags"
}

# Main script
main() {
    local bump_type=""
    local dry_run=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_usage
                exit 0
                ;;
            -t|--type)
                if [ -z "$2" ]; then
                    log_error "Type flag requires an argument"
                    show_usage
                    exit 1
                fi
                bump_type="$2"
                shift 2
                ;;
            --dry-run)
                dry_run=true
                shift
                ;;
            patch|minor|major)
                if [ -n "$bump_type" ]; then
                    log_error "Bump type already specified"
                    show_usage
                    exit 1
                fi
                bump_type="$1"
                shift
                ;;
            *)
                log_error "Unknown argument: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Validate arguments
    if [ -z "$bump_type" ]; then
        log_error "Bump type is required"
        show_usage
        exit 1
    fi
    
    # Check if project file exists
    if [ ! -f "$PROJECT_FILE" ]; then
        log_error "Project file not found: $PROJECT_FILE"
        log_info "Make sure you're running this script from the repository root"
        exit 1
    fi
    
    # Get current version
    local current_version
    current_version=$(get_current_version)
    
    # Calculate new version
    local new_version
    new_version=$(bump_version "$current_version" "$bump_type")
    
    # Show what will be done
    log_info "Current version: $current_version"
    log_info "New version: $new_version"
    log_info "Bump type: $bump_type"
    
    if [ "$dry_run" = "true" ]; then
        log_warning "DRY RUN - No changes will be made"
    fi
    
    
    # Update files
    update_project_file "$new_version" "$dry_run"
    update_changelog "$new_version" "$dry_run"
    
    if [ "$dry_run" = "false" ]; then
        commit_changes "$new_version" "$dry_run"
        log_success "Version bump completed successfully!"
        log_info "New version: $new_version"
    else
        log_info "Dry run completed. No changes were made."
    fi
}

# Run main function
main "$@"