#!/bin/bash

# NuGet publishing script for TiMoch.Orleans.TestUtilities
# Usage: ./scripts/publish-nuget.sh [--dry-run] [--api-key KEY] [--source URL]

set -e

# Configuration
PROJECT_FILE="src/TiMoch.Orleans.TestUtilities/TiMoch.Orleans.TestUtilities.csproj"
OUTPUT_DIR="artifacts/nuget"
DEFAULT_SOURCE="https://api.nuget.org/v3/index.json"

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
Usage: $0 [OPTIONS]

Publish TiMoch.Orleans.TestUtilities NuGet package.

Options:
    --dry-run           Build and validate package without publishing
    --api-key KEY       NuGet API key (can also use NUGET_API_KEY env var)
    --source URL        NuGet source URL (default: nuget.org)
    --skip-build        Skip building and use existing package
    --force             Skip confirmation prompts
    -h, --help          Show this help message

Environment Variables:
    NUGET_API_KEY       NuGet API key (alternative to --api-key)

Examples:
    $0 --dry-run                    # Build and validate without publishing
    $0 --api-key abc123             # Publish with API key
    $0                              # Interactive mode (will prompt for API key)

Prerequisites:
    - .NET SDK installed
    - NuGet CLI tools available
    - Valid NuGet API key for publishing
EOF
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK not found. Please install .NET SDK."
        exit 1
    fi
    
    local dotnet_version
    dotnet_version=$(dotnet --version 2>/dev/null || echo "unknown")
    log_info "Found .NET SDK version: $dotnet_version"
    
    # Check project file
    if [ ! -f "$PROJECT_FILE" ]; then
        log_error "Project file not found: $PROJECT_FILE"
        log_info "Make sure you're running this script from the repository root"
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

get_package_version() {
    local version=$(grep -oP '<Version>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || echo "")
    
    if [ -z "$version" ]; then
        version=$(grep -oP '<PackageVersion>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || echo "")
    fi
    
    if [ -z "$version" ]; then
        log_error "Could not find version in $PROJECT_FILE"
        exit 1
    fi
    
    echo "$version"
}

validate_version() {
    local version="$1"
    if [[ ! $version =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9-]+)?$ ]]; then
        log_error "Invalid version format: $version"
        log_info "Expected format: major.minor.patch[-prerelease]"
        exit 1
    fi
}

clean_output_directory() {
    local dry_run="$1"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would clean output directory: $OUTPUT_DIR"
        return
    fi
    
    if [ -d "$OUTPUT_DIR" ]; then
        log_info "Cleaning output directory: $OUTPUT_DIR"
        rm -rf "$OUTPUT_DIR"
    fi
    
    mkdir -p "$OUTPUT_DIR"
}

build_package() {
    local dry_run="$1"
    local version="$2"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would build NuGet package for version $version"
        log_info "Build command: dotnet pack \"$PROJECT_FILE\" --configuration Release --output \"$OUTPUT_DIR\" --verbosity normal"
        return 0
    fi
    
    log_info "Building NuGet package for version $version..."
    
    # Build in Release configuration
    if ! dotnet pack "$PROJECT_FILE" \
        --configuration Release \
        --output "$OUTPUT_DIR" \
        --verbosity normal; then
        log_error "Failed to build NuGet package"
        return 1
    fi
    
    local package_file="$OUTPUT_DIR/TiMoch.Orleans.TestUtilities.$version.nupkg"
    if [ ! -f "$package_file" ]; then
        log_error "Package file not found: $package_file"
        return 1
    fi
    
    log_success "Successfully built package: $package_file"
    return 0
}

validate_package() {
    local version="$1"
    local dry_run="$2"
    local package_file="$OUTPUT_DIR/TiMoch.Orleans.TestUtilities.$version.nupkg"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would validate package: $package_file"
        return 0
    fi
    
    if [ ! -f "$package_file" ]; then
        log_error "Package file not found: $package_file"
        return 1
    fi
    
    log_info "Validating package: $package_file"
    
    # Check package size
    local size
    size=$(stat -f%z "$package_file" 2>/dev/null || stat -c%s "$package_file" 2>/dev/null || echo "0")
    if [ "$size" -lt 1000 ]; then
        log_warning "Package size is unusually small: $size bytes"
    else
        log_info "Package size: $size bytes"
    fi
    
    # Verify package contents using dotnet
    log_info "Package contents:"
    if command -v unzip &> /dev/null; then
        unzip -l "$package_file" | head -20
    else
        log_warning "unzip not available, cannot show package contents"
    fi
    
    log_success "Package validation completed"
    return 0
}

check_existing_version() {
    local version="$1"
    local source="$2"
    local dry_run="$3"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would check if version $version already exists on $source"
        return 0
    fi
    
    log_info "Checking if version $version already exists on $source..."
    
    # Try to search for the package version
    if dotnet nuget search "TiMoch.Orleans.TestUtilities" --exact-match --source "$source" --format json >/dev/null 2>&1; then
        log_warning "Package search completed. Manual verification recommended."
    else
        log_info "Package search not available or package doesn't exist yet"
    fi
    
    return 0
}

publish_package() {
    local version="$1"
    local api_key="$2"
    local source="$3"
    local dry_run="$4"
    local force="$5"
    
    local package_file="$OUTPUT_DIR/TiMoch.Orleans.TestUtilities.$version.nupkg"
    
    if [ "$dry_run" = "true" ]; then
        log_info "Would publish package to $source"
        log_info "Package: $package_file"
        log_info "API key: ${api_key:0:8}..."
        return 0
    fi
    
    if [ ! -f "$package_file" ]; then
        log_error "Package file not found: $package_file"
        return 1
    fi
    
    log_info "Publishing package to $source..."
    log_info "Package: $package_file"
    log_info "Version: $version"
    
    # Confirm publication if not forced
    if [ "$force" = "false" ]; then
        echo -n "Proceed with publication? (y/N): "
        read -r confirmation
        if [[ ! $confirmation =~ ^[Yy]$ ]]; then
            log_info "Publication cancelled"
            return 0
        fi
    fi
    
    # Publish package
    if ! dotnet nuget push "$package_file" \
        --api-key "$api_key" \
        --source "$source" \
        --timeout 300; then
        log_error "Failed to publish package"
        return 1
    fi
    
    log_success "Successfully published TiMoch.Orleans.TestUtilities $version to $source"
    log_info "It may take a few minutes for the package to appear in search results"
    
    return 0
}

# Main script
main() {
    local dry_run=false
    local api_key=""
    local source="$DEFAULT_SOURCE"
    local skip_build=false
    local force=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            -h|--help)
                show_usage
                exit 0
                ;;
            --dry-run)
                dry_run=true
                shift
                ;;
            --api-key)
                if [ -z "$2" ]; then
                    log_error "API key flag requires an argument"
                    show_usage
                    exit 1
                fi
                api_key="$2"
                shift 2
                ;;
            --source)
                if [ -z "$2" ]; then
                    log_error "Source flag requires an argument"
                    show_usage
                    exit 1
                fi
                source="$2"
                shift 2
                ;;
            --skip-build)
                skip_build=true
                shift
                ;;
            --force)
                force=true
                shift
                ;;
            *)
                log_error "Unknown argument: $1"
                show_usage
                exit 1
                ;;
        esac
    done
    
    # Check prerequisites
    check_prerequisites
    
    # Get package version
    local version
    version=$(get_package_version)
    validate_version "$version"
    
    log_info "Package: TiMoch.Orleans.TestUtilities"
    log_info "Version: $version"
    log_info "Target source: $source"
    
    if [ "$dry_run" = "true" ]; then
        log_warning "DRY RUN - No actual changes will be made"
    fi
    
    # Get API key if not provided and not in dry run
    if [ "$dry_run" = "false" ] && [ -z "$api_key" ]; then
        # Try environment variable first
        if [ -n "$NUGET_API_KEY" ]; then
            api_key="$NUGET_API_KEY"
            log_info "Using API key from NUGET_API_KEY environment variable"
        else
            echo -n "Enter NuGet API key: "
            read -rs api_key
            echo
            
            if [ -z "$api_key" ]; then
                log_error "API key is required for publishing"
                exit 1
            fi
        fi
    fi
    
    # Clean and prepare output directory
    if [ "$skip_build" = "false" ]; then
        clean_output_directory "$dry_run"
        
        # Build package
        if ! build_package "$dry_run" "$version"; then
            exit 1
        fi
    fi
    
    # Validate package
    if ! validate_package "$version" "$dry_run"; then
        exit 1
    fi
    
    # Check if version already exists
    check_existing_version "$version" "$source" "$dry_run"
    
    # Publish package
    if ! publish_package "$version" "$api_key" "$source" "$dry_run" "$force"; then
        exit 1
    fi
    
    if [ "$dry_run" = "false" ]; then
        log_success "NuGet publishing completed successfully!"
        log_info "Package: TiMoch.Orleans.TestUtilities $version"
        log_info "Published to: $source"
        log_info "View package: https://www.nuget.org/packages/TiMoch.Orleans.TestUtilities/$version"
    else
        log_info "Dry run completed successfully. Ready for actual publishing."
    fi
}

# Run main function
main "$@"