# ConverSense - Smart Audio Companion for Intelligent Conversation Management

> Transform every conversation into actionable insights with AI-powered audio analysis and real-time transcription

## 🎯 Problem Statement

In our fast-paced world, we have countless important conversations daily - meetings, brainstorming sessions, interviews, and casual discussions that contain valuable information, decisions, and action items. However, most of this valuable content is lost due to:

- **Information Overload**: Too many conversations to remember or process effectively
- **Manual Note-Taking**: Time-consuming and error-prone note-taking during conversations
- **Lost Context**: Important details, decisions, and action items get forgotten over time
- **Inefficient Follow-ups**: Difficulty tracking commitments and tasks mentioned in conversations
- **Lack of Searchability**: No way to search through past conversations for specific information
- **Missed Insights**: Unable to identify patterns and themes across multiple conversations

ConverSense solves these problems by providing an intelligent, wearable solution that automatically captures, processes, and analyzes your conversations in real-time.

## 💡 How It Works

ConverSense consists of two main components working in perfect harmony:

### 1. Smart Pendant Device
- **Hardware**: M5StickC PLUS2 with ESP32-PICO-V3-02 processor
- **Audio Capture**: High-quality 16kHz audio recording
- **Connectivity**: Bluetooth Low Energy (BLE) for seamless phone pairing
- **Interface**: Minimal LCD display showing recording status and connection state

### 2. Cross-Platform Mobile/Desktop App
- **Technology**: .NET MAUI Hybrid with Blazor components
- **Real-time Processing**: Live audio streaming and transcription
- **AI Integration**: Azure Speech Service for transcription and diarization, GPT for insights
- **Local Storage**: SQLite database for privacy and offline access
- **Cross-platform**: Runs on Android and Windows

## 🏗️ System Architecture

```mermaid
graph TB
    subgraph "Hardware Layer"
        A[M5StickC PLUS2 Device]
        A1[ESP32-PICO-V3-02]
        A2[Built-in Microphone]
        A3[BLE Module]
        A4[1.14" TFT LCD Display]
        A5[FreeRTOS Audio Buffer]
        A1 --> A2
        A1 --> A3
        A1 --> A4
        A1 --> A5
        A5 --> A3
    end
    
    subgraph "MAUI Hybrid Application"
        B[Platform Services]
        B1[Windows/Android Orchestration]
        B2[BLE/Bluetooth Classic Service]
        B3[Audio Pipeline Manager]
        B4[Azure Speech Service]
        B5[Conversation Insight Service]
        B6[Audio Storage Service]
        B7[Entity Framework Repositories]
        B8[SQLite Database]
        B1 --> B3
        B2 --> B3
        B3 --> B4
        B3 --> B6
        B5 --> B7
        B7 --> B8
    end
    
    subgraph "Blazor UI Components"
        C[Home.razor]
        C1[Conversation.razor]
        C2[Tasks.razor]
        C3[History.razor]
        C4[DayInsight.razor]
        C5[MudBlazor Framework]
        C --> C5
        C1 --> C5
        C2 --> C5
        C3 --> C5
        C4 --> C5
    end
    
    subgraph "AI Services"
        D[Azure Speech Service]
        D1[Real-time Transcription]
        D2[Speaker Diarization]
        E[Azure OpenAI]
        E1[GPT-4 Analysis]
        E2[Content Summarization]
        E3[Task Extraction]
        D --> D1
        D --> D2
        E --> E1
        E --> E2
        E --> E3
    end
    
    A3 -.->|BLE Audio Stream| B2
    B4 --> D
    B5 --> E
    C5 --> B1
    D1 --> B3
    D2 --> B3
    E1 --> B5
    E2 --> B5
    E3 --> B5
```



## 🔄 Data Flow & Processing Pipeline

```mermaid
sequenceDiagram
    participant P as M5StickC PLUS2
    participant BLE as BLE Service
    participant APM as Audio Pipeline Manager
    participant ASS as Audio Storage Service
    participant AzS as Azure Speech Service
    participant OS as Orchestration Service
    participant CIS as Conversation Insight Service
    participant AzOAI as Azure OpenAI
    participant EF as EF Repository
    participant DB as SQLite Database
    
    P->>BLE: Audio Stream (16kHz BLE packets)
    BLE->>APM: Audio Data Channel
    APM->>ASS: Store Audio File
    APM->>AzS: Real-time Audio Chunks
    AzS->>APM: Transcribed Text + Speaker ID
    APM->>OS: Transcript Updates
    OS->>CIS: Generate Insights (on-demand)
    CIS->>AzOAI: Conversation Analysis Request
    AzOAI->>CIS: Summary, Tasks, Topics, Timeline
    CIS->>OS: Structured Insights
    OS->>EF: Save Conversation Record
    EF->>DB: Persist to SQLite
    OS->>UI: Update Blazor Components
```

## ✨ Key Features & Benefits

### 🎤 **Seamless Audio Capture**
- Hands-free recording with wearable pendant device
- High-quality 16kHz audio capture optimized for speech
- Automatic noise filtering and audio enhancement
- Real-time audio streaming via BLE connection

### 🤖 **AI-Powered Intelligence**
- **Real-time Transcription**: Convert speech to text with Azure Speech Service
- **Speaker Diarization**: Advanced speaker identification and separation
- **Smart Summarization**: Generate concise summaries of key discussion points
- **Automatic Task Extraction**: Identify and track action items and commitments
- **Topic Analysis**: Extract and categorize main themes and subjects
- **Timeline Generation**: Create chronological event timelines for long conversations

### 📊 **Comprehensive Insights**
- Daily conversation overviews and patterns
- Searchable conversation archive
- Task management and follow-up tracking
- Speaker analytics and participation metrics
- Conversation quality and engagement analysis

### 🔒 **Privacy & Security**
- **No Cloud Dependencies**: Conversations stored locally on your device
- **Recording Indicator**: Visual cue when recording is active


### 🌐 **Cross-Platform Compatibility**
- Native mobile apps for Android and iOS
- Desktop applications for Windows
- Synchronized data across all your devices
- Responsive web-based interface

## 📱 Application Screenshots

### Pendant Device Interface
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/embedded_1.jpg" alt="Pendant Device - Home Screen" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/embedded_2.jpg" alt="Pendant Device - Connection Status" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/embedded_recording.jpg" alt="Pendant Device - Recording Mode" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

### Mobile Application Interface

#### Main Recording & Control
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/mobile_landing.jpg" alt="Mobile App - Landing Screen" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_recording_1.jpg" alt="Mobile App - Recording Interface" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_recoding_2.jpg" alt="Mobile App - Recording Controls" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

#### Conversation Analysis & Insights
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/mobile_conversation_transcript.jpg" alt="Conversation Transcript" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_conversation_summary.jpg" alt="Conversation Summary" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_conversation_summary_1.jpg" alt="Detailed Summary View" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_conversation_summary_2.jpg" alt="Summary Insights" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

#### Timeline & Task Management
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/mobile_conversation_timeline.jpg" alt="Conversation Timeline" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_conversation_tasks.jpg" alt="Extracted Tasks" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_tasks.jpg" alt="Task Management" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

#### Daily Journal & Insights
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/mobile_journal_1.jpg" alt="Daily Journal Overview" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_journal_2.jpg" alt="Journal Insights" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_journal_3.jpg" alt="Daily Patterns" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

#### Search & History
<div style="display: flex; gap: 10px; flex-wrap: wrap;">
  <img src="/Resources/mobile_history_search.jpg" alt="Conversation History Search" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
  <img src="/Resources/mobile_generating_insight.jpg" alt="AI Insight Generation" width="200" height="auto" style="border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.2);">
</div>

## 🛠️ Technical Implementation

### Hardware Architecture
- **Microcontroller**: ESP32-PICO-V3-02 (240MHz dual-core, 520KB SRAM)
- **Audio Sampling**: 16kHz, 16-bit, mono channel recording
- **Connectivity**: Bluetooth Low Energy 5.0 with custom audio streaming protocol
- **Display**: 1.14" TFT LCD (135x240 pixels) for status indication
- **Power Management**: Optimized for extended battery life with sleep modes
- **Audio Buffer**: 5-chunk circular buffer with FreeRTOS stream management

### Software Stack

```mermaid
graph TD
    subgraph "Presentation Layer"
        A[.NET MAUI 9.0 Hybrid]
        A --> B[Blazor Server Components]
        A --> C[Platform-Specific Services]
        B --> D[MudBlazor UI Framework]
        B --> E[Home, Conversation, Tasks, History, DayInsight Pages]
    end
    
    subgraph "Service Layer"
        F[Orchestration Services]
        F --> G[Windows/Android Platform Services]
        F --> H[Audio Pipeline Manager]
        F --> I[Connection Services]
        I --> J[BLE Service]
        I --> K[Bluetooth Classic Service] 
        I --> L[Mock Service for Testing]
        H --> M[Audio Storage Service]
        H --> N[Azure Speech Service Integration]
    end
    
    subgraph "Data Access Layer"
        O[Entity Framework Core]
        O --> P[EF Conversation Repository]
        O --> Q[EF Day Journal Repository]
        O --> R[SQLite Database]
        P --> S[Conversation Records]
        P --> T[Transcript Entries]
        P --> U[Action Items]
        P --> V[Timeline Events]
        Q --> W[Daily Journal Records]
    end
    
    subgraph "AI Integration Layer"
        X[Azure Speech Service]
        X --> Y[Real-time Transcription]
        X --> Z[Speaker Diarization]
        AA[Azure OpenAI]
        AA --> BB[Conversation Insight Service]
        AA --> CC[Daily Journal Insight Service]
        BB --> DD[Content Analysis]
        BB --> EE[Task Extraction]
        BB --> FF[Topic Classification]
    end
    
    A --> F
    F --> O
    H --> X
    BB --> AA
    CC --> AA
```

## Desktop Demo
[![Desktop Demo](/Resources/desktop_windows_demo.gif)]

### Key Components

#### 🎯 **Orchestration Service Architecture**
```mermaid
graph LR
    subgraph "Platform Orchestration"
        A[IOrchestrationService]
        A --> B[WindowsOrchestrationService]
        A --> C[AndroidOrchestrationService]
    end
    
    subgraph "Core Pipeline"
        D[AudioPipelineManager]
        D --> E[Channel<byte[]> Audio Data]
        D --> F[Inactivity Timer]
        D --> G[ConversationRecord]
        D --> H[DayRecord]
    end
    
    subgraph "Connection Layer"
        I[IConnectionService]
        I --> J[BLEService]
        I --> K[BluetoothClassicService]
        I --> L[MockConnectionService]
    end
    
    subgraph "Transcription Layer"
        M[ITranscriptionService]
        M --> N[SpeechTranscriptionService]
        M --> O[OpenAITranscriptionService]
        M --> P[MockTranscriptionService]
    end
    
    B --> D
    C --> D
    D --> I
    D --> M
```

#### 🎯 **Orchestration Service**
Central coordinator managing the application lifecycle:
- Device connection management
- Audio stream processing coordination
- Real-time transcription orchestration
- AI insight generation scheduling
- State management across UI components

#### 🔊 **Audio Processing Pipeline**
- **Capture**: 16kHz audio sampling from pendant device via FreeRTOS stream buffer
- **Buffering**: Multi-chunk circular buffer with Channel<byte[]> for smooth streaming
- **Transmission**: BLE packet optimization for audio data with MTU size management
- **Processing**: Real-time audio enhancement and noise reduction
- **Storage**: Local audio file storage via AudioStorageService

#### 🤖 **AI Integration Services**
- **Transcription Service**: Azure Speech Service with ConversationTranscriber for real-time processing
- **Speaker Diarization**: Advanced speaker identification with conversation turn detection
- **Insight Service**: Azure OpenAI GPT-4 powered conversation analysis using IChatClient
- **Content Analysis**: Smart extraction of topics, tasks, and summaries via structured prompts

## 💼 Business Benefits

### For Professionals
- **Meeting Efficiency**: Never miss important decisions or action items
- **Client Relations**: Better follow-up on client conversations and commitments
- **Knowledge Management**: Build a searchable knowledge base of professional interactions
- **Performance Tracking**: Analyze communication patterns and effectiveness

### For Teams
- **Collaboration**: Share conversation insights and action items seamlessly
- **Accountability**: Track commitments and follow-through across team members
- **Documentation**: Automatic meeting minutes and decision logs
- **Process Improvement**: Identify communication bottlenecks and opportunities

### For Personal Use
- **Memory Enhancement**: Never forget important personal conversations
- **Relationship Building**: Track meaningful interactions with friends and family
- **Learning**: Review and reflect on educational or mentoring conversations
- **Goal Tracking**: Monitor progress on personal development discussions




## 🏗️ Development & Architecture

### Project Structure
```
SmartPendant/
├── SmartPendant.Embedded/          # ESP32 firmware
│   ├── src/main.cpp               # Main application logic
│   ├── platformio.ini             # Build configuration
│   └── lib/                       # Dependencies
├── SmartPendant.MAUIHybrid/       # Cross-platform app
│   ├── Components/                # Blazor UI components
│   ├── Services/                  # Core business logic
│   ├── Data/                      # Database entities
│   ├── Abstractions/              # Service interfaces
│   └── Platforms/                 # Platform-specific code
└── Resources/                     # Documentation assets
```

### Technology Stack
- **Embedded**: C++ with ESP-IDF and Arduino framework
- **Mobile**: .NET MAUI 9.0 with Blazor Server components
- **UI Framework**: MudBlazor for Material Design components
- **Database**: SQLite with Entity Framework Core
- **AI Integration**: 
  - Azure Speech Service for transcription and speaker diarization
  - OpenAI GPT-4 for conversation analysis and insights
- **Communication**: Bluetooth Low Energy (BLE)

## 🌟 Future Roadmap

### Planned Features
- **Map View**: Location based transcription and analysis
- **Multi-language Support**: Transcription and analysis in 50+ languages
- **Audio Memories**: Like photos, users can save and revisit important audio snippets (like on this day...)
- **Smart Notifications**: Proactive reminders for action items


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## 🙏 Acknowledgments

- **M5Stack** for the excellent M5StickC PLUS2 hardware platform
- **Microsoft Azure** for providing robust Speech Service with advanced diarization capabilities
- **OpenAI** for providing powerful AI APIs for conversation analysis and insights
- **Microsoft** for the .NET MAUI framework enabling cross-platform development
- **MudBlazor** community for the beautiful UI component library

---

**Built with ❤️ for better conversations and smarter insights**

