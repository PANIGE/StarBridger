[![Build](https://github.com/PANIGE/StarBridger/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/OWNER/StarBridger/actions/workflows/build.yml)

[![Linter](https://github.com/PANIGE/StarBridger/actions/workflows/linter.yml/badge.svg)](https://github.com/OWNER/StarBridger/actions/workflows/linter.yml)

![License](https://img.shields.io/badge/license-MIT-blue)

# StarBridger

**An open-source companion bridge for Star Citizen telemetry.**

*StarBridger connects Star Citizen's experimental telemetry to external applications through a modular .NET SDK.*

MIT licensed.

## What is StarBridger?

StarBridger is a small collection of applications and libraries designed to make Star Citizen telemetry accessible to developers and companion tools.

The goal is simple: let developers build interesting things around game telemetry without having to reinvent the entire communication layer. (I already did that part)

StarBridger is an unofficial community project and is not affiliated with Cloud Imperium Games or Roberts Space Industries.

## Features

- **Host** — Manages the Star Citizen telemetry connection and provides local IPC services.
- **SDK** — Provides a typed .NET client, telemetry contexts and shell commands.
- **UI** — A lightweight desktop application for managing the Host.
- **Shell** — A command-line interface for inspecting telemetry and interacting with the Host.
- **Contracts** — Shared versioned models for communication and telemetry data.

## Getting started

Download the latest Windows release from the GitHub Releases page.

Choose between the portable ZIP and the traditional MSI installer.

StarBridger is currently experimental. Its functionality depends on Star Citizen's evolving telemetry API.

## Architecture

Star Citizen => Telemetry => Host => IPC => SDK => Applications

The SDK is designed to remain independent from the Host implementation. Applications interact with the SDK rather than directly managing the telemetry connection.

Local communication uses named pipes. A Web/LAN transport is planned for future versions.

## Privacy and philosophy

Your computer is your home. StarBridger is a guest.

StarBridger does not require a cloud account or upload your game telemetry to a ANY collection server.

The application is designed to be transparent about the data it processes, the services it runs and the files it creates.

No unnecessary persistence. No forced updates. No surprise background services.

## Development

This repository contains the Host, SDK, UI, Shell, Contracts, supporting infrastructure, and automated tests.

Builds and releases are managed using GitHub Actions and MSBuild.

Contributions and issue reports are welcome.

## AI Usage & Authorship

Since AI-assisted development is a topic worth being transparent about, here's how AI was used in StarBridger.

**TL;DR: Human-written implementation, AI-assisted development.**

The code intended for other developers to use, particularly the SDK and its public APIs, was written by hand. AI was used to review that code, not to generate its implementation.

AI assistance was primarily used for:

- **Unit testing:** AI wrote a significant portion of the unit tests.
- **Code review:** A second pair of eyes for potential bugs, edge cases and questionable decisions.
- **Architecture discussions:** Exploring alternatives, challenging assumptions and sanity-checking design choices.
- **Development tooling:** Assistance with documentation, CI/CD and packaging scripts.

The implementation and architectural decisions remain under human control.

AI-generated suggestions are not automatically treated as correct. They are suggestions, not authority.

**Why mention this?**

Because this project is open source, and transparency should include how it was developed.

You are welcome to inspect the code, challenge its design, report issues or contribute improvements.

The code should speak for itself, regardless of which tools helped along the way.

## License

StarBridger is released under the **MIT License**. See [LICENSE](LICENSE).

Star Citizen and related trademarks and assets belong to their respective owners.