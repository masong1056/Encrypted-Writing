import SwiftUI
import Cocoa
import ApplicationServices
import Carbon
import Combine

// MARK: - Main App
@main
struct CaesarCipherApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
        .defaultSize(width: 300, height: 320)
    }
}

// MARK: - App Delegate
class AppDelegate: NSObject, NSApplicationDelegate {
    var keyMonitor: Any?
    
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return false
    }
    
    func applicationDidFinishLaunching(_ notification: Notification) {
        // Request accessibility on launch
        let options: NSDictionary = [kAXTrustedCheckOptionPrompt.takeRetainedValue() as String: true]
        AXIsProcessTrustedWithOptions(options)
        
        // Configure window to be fixed size
        DispatchQueue.main.async {
            if let window = NSApplication.shared.windows.first {
                window.styleMask.remove(.resizable)
                window.setContentSize(NSSize(width: 300, height: 320))
            }
        }
        
        // Set up global keyboard shortcut: Cmd+Shift+Option+C
        keyMonitor = NSEvent.addGlobalMonitorForEvents(matching: .keyDown) { event in
            // Check for Cmd+Shift+Option+C
            if event.modifierFlags.contains([.command, .shift, .option]) &&
               event.charactersIgnoringModifiers == "c" {
                NotificationCenter.default.post(name: .toggleCipher, object: nil)
            }
        }
    }
}

// MARK: - Notification Extension
extension Notification.Name {
    static let toggleCipher = Notification.Name("toggleCipher")
}

// MARK: - UI
struct ContentView: View {
    @StateObject private var monitor = KeyboardMonitor()
    @State private var showingConverter = false
    
    var body: some View {
        ZStack {
            LinearGradient(
                colors: [Color(nsColor: .windowBackgroundColor), Color(nsColor: .controlBackgroundColor)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()
            
            VStack(spacing: 24) {
                Text("Encrypted Writing")
                    .font(.system(size: 22, weight: .bold))
                    .foregroundStyle(.primary)
                
                VStack(spacing: 16) {
                    HStack(spacing: 16) {
                        Text("Enable")
                            .font(.system(size: 15, weight: .medium))
                        
                        Spacer()
                        
                        Toggle("", isOn: $monitor.isEnabled)
                            .toggleStyle(.switch)
                            .labelsHidden()
                    }
                    .padding(.horizontal, 20)
                    .padding(.vertical, 12)
                    .background(Color(nsColor: .controlBackgroundColor).opacity(0.5))
                    .cornerRadius(12)
                    
                    HStack(spacing: 10) {
                        Circle()
                            .fill(monitor.isEnabled ? Color.green : Color.gray)
                            .frame(width: 10, height: 10)
                            .shadow(color: monitor.isEnabled ? .green.opacity(0.5) : .clear, radius: 4)
                        
                        Text(monitor.isEnabled ? "Active" : "Inactive")
                            .font(.system(size: 13, weight: .medium))
                            .foregroundColor(.secondary)
                        
                        Spacer()
                        
                        Text("⌘⇧⌥C")
                            .font(.system(size: 11, weight: .semibold, design: .monospaced))
                            .foregroundColor(.secondary)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background(Color.gray.opacity(0.15))
                            .cornerRadius(6)
                    }
                    .padding(.horizontal, 4)
                }
                .padding(.horizontal)
                
                Button {
                    showingConverter = true
                } label: {
                    HStack {
                        Image(systemName: "arrow.left.arrow.right.circle.fill")
                        Text("Text Encoder/Decoder")
                            .font(.system(size: 14, weight: .semibold))
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 10)
                }
                .buttonStyle(.borderedProminent)
                .cornerRadius(10)
                .padding(.horizontal)
                
                if !monitor.hasPermission {
                    VStack(spacing: 10) {
                        Text("⚠️ Accessibility Required")
                            .font(.system(size: 12, weight: .semibold))
                            .foregroundColor(.orange)
                        
                        Button("Check Again") {
                            monitor.checkPermission()
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.small)
                        .cornerRadius(8)
                    }
                    .padding(.horizontal)
                    .padding(.vertical, 12)
                    .background(Color.orange.opacity(0.1))
                    .cornerRadius(12)
                    .padding(.horizontal)
                }
            }
            .padding(.vertical, 24)
        }
        .frame(width: 300, height: 320)
        .fixedSize()
        .onAppear {
            monitor.checkPermission()
        }
        .onReceive(NotificationCenter.default.publisher(for: .toggleCipher)) { _ in
            monitor.isEnabled.toggle()
        }
        .sheet(isPresented: $showingConverter) {
            ConverterView(isPresented: $showingConverter)
        }
    }
}

// MARK: - Converter View
struct ConverterView: View {
    @Binding var isPresented: Bool
    @State private var inputText = ""
    @State private var outputText = ""
    @State private var mode: ConversionMode = .encode
    
    enum ConversionMode {
        case encode, decode
    }
    
    var body: some View {
        ZStack {
            LinearGradient(
                colors: [Color(nsColor: .windowBackgroundColor), Color(nsColor: .controlBackgroundColor)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()
            
            VStack(spacing: 0) {
                // Header with close button
                HStack {
                    Text("Caesar Cipher Converter")
                        .font(.system(size: 20, weight: .bold))
                    
                    Spacer()
                    
                    Button {
                        isPresented = false
                    } label: {
                        Image(systemName: "xmark.circle.fill")
                            .font(.system(size: 20))
                            .foregroundStyle(.secondary)
                    }
                    .buttonStyle(.plain)
                    .help("Close")
                }
                .padding(.horizontal, 24)
                .padding(.top, 24)
                .padding(.bottom, 20)
                
                // Content
                VStack(spacing: 20) {
                    Picker("Mode", selection: $mode) {
                        Text("Encode").tag(ConversionMode.encode)
                        Text("Decode").tag(ConversionMode.decode)
                    }
                    .pickerStyle(.segmented)
                    .padding(.horizontal, 24)
                    
                    VStack(alignment: .leading, spacing: 10) {
                        HStack {
                            Image(systemName: "square.and.pencil")
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                            Text("Input:")
                                .font(.system(size: 13, weight: .semibold))
                        }
                        
                        TextEditor(text: $inputText)
                            .font(.system(size: 14))
                            .frame(height: 120)
                            .padding(8)
                            .background(Color(nsColor: .textBackgroundColor))
                            .cornerRadius(10)
                            .overlay(
                                RoundedRectangle(cornerRadius: 10)
                                    .stroke(Color.gray.opacity(0.2), lineWidth: 1)
                            )
                    }
                    .padding(.horizontal, 24)
                    
                    HStack(spacing: 12) {
                        Button {
                            convert()
                        } label: {
                            HStack {
                                Image(systemName: mode == .encode ? "arrow.right.circle.fill" : "arrow.left.circle.fill")
                                Text(mode == .encode ? "Encode" : "Decode")
                                    .font(.system(size: 14, weight: .semibold))
                            }
                            .frame(maxWidth: .infinity)
                            .padding(.vertical, 8)
                        }
                        .buttonStyle(.borderedProminent)
                        .cornerRadius(10)
                        
                        Button {
                            inputText = ""
                            outputText = ""
                        } label: {
                            HStack {
                                Image(systemName: "trash")
                                Text("Clear")
                                    .font(.system(size: 14, weight: .medium))
                            }
                            .padding(.vertical, 8)
                            .padding(.horizontal, 12)
                        }
                        .buttonStyle(.bordered)
                        .cornerRadius(10)
                        
                        Button {
                            NSPasteboard.general.clearContents()
                            NSPasteboard.general.setString(outputText, forType: .string)
                        } label: {
                            HStack {
                                Image(systemName: "doc.on.doc")
                                Text("Copy")
                                    .font(.system(size: 14, weight: .medium))
                            }
                            .padding(.vertical, 8)
                            .padding(.horizontal, 12)
                        }
                        .buttonStyle(.bordered)
                        .cornerRadius(10)
                        .disabled(outputText.isEmpty)
                    }
                    .padding(.horizontal, 24)
                    
                    VStack(alignment: .leading, spacing: 10) {
                        HStack {
                            Image(systemName: "doc.text")
                                .font(.system(size: 12))
                                .foregroundColor(.secondary)
                            Text("Output:")
                                .font(.system(size: 13, weight: .semibold))
                        }
                        
                        TextEditor(text: $outputText)
                            .font(.system(size: 14))
                            .frame(height: 120)
                            .padding(8)
                            .background(Color(nsColor: .textBackgroundColor).opacity(0.5))
                            .cornerRadius(10)
                            .overlay(
                                RoundedRectangle(cornerRadius: 10)
                                    .stroke(Color.gray.opacity(0.2), lineWidth: 1)
                            )
                            .disabled(true)
                    }
                    .padding(.horizontal, 24)
                    
                    Spacer()
                }
            }
        }
        .frame(width: 550, height: 500)
        .fixedSize()
    }
    
    func convert() {
        let shift = mode == .encode ? 7 : -7
        outputText = caesarCipher(inputText, shift: shift)
    }
    
    func caesarCipher(_ text: String, shift: Int) -> String {
        return text.map { char in
            guard char.isLetter else { return char }
            
            let isUpper = char.isUppercase
            let base = isUpper ? Character("A") : Character("a")
            let offset = Int(char.asciiValue!) - Int(base.asciiValue!)
            let shifted = (offset + shift + 26) % 26
            return Character(UnicodeScalar(Int(base.asciiValue!) + shifted)!)
        }.map { String($0) }.joined()
    }
}

// MARK: - Keyboard Monitor
class KeyboardMonitor: ObservableObject {
    @Published var isEnabled = false {
        didSet {
            isEnabled ? start() : stop()
        }
    }
    
    @Published var hasPermission = false
    
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    
    func checkPermission() {
        DispatchQueue.main.async {
            self.hasPermission = AXIsProcessTrusted()
        }
    }
    
    func start() {
        guard AXIsProcessTrusted() else {
            let options: NSDictionary = [kAXTrustedCheckOptionPrompt.takeRetainedValue() as String: true]
            AXIsProcessTrustedWithOptions(options)
            isEnabled = false
            return
        }
        
        stop()
        
        let mask = CGEventMask(1 << CGEventType.keyDown.rawValue) | CGEventMask(1 << CGEventType.keyUp.rawValue)
        
        guard let tap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: mask,
            callback: { proxy, type, event, refcon -> Unmanaged<CGEvent>? in
                guard let refcon = refcon else { return Unmanaged.passUnretained(event) }
                let monitor = Unmanaged<KeyboardMonitor>.fromOpaque(refcon).takeUnretainedValue()
                return monitor.handleEvent(event: event, type: type)
            },
            userInfo: Unmanaged.passUnretained(self).toOpaque()
        ) else {
            isEnabled = false
            return
        }
        
        eventTap = tap
        runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
        CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .commonModes)
        CGEvent.tapEnable(tap: tap, enable: true)
    }
    
    func stop() {
        if let tap = eventTap {
            CGEvent.tapEnable(tap: tap, enable: false)
            if let source = runLoopSource {
                CFRunLoopRemoveSource(CFRunLoopGetCurrent(), source, .commonModes)
            }
        }
        eventTap = nil
        runLoopSource = nil
    }
    
    func handleEvent(event: CGEvent, type: CGEventType) -> Unmanaged<CGEvent>? {
        guard type == .keyDown else {
            return Unmanaged.passUnretained(event)
        }
        
        let flags = event.flags
        
        // Don't remap if Cmd, Ctrl, or Option is pressed
        if flags.contains(.maskCommand) || flags.contains(.maskControl) || flags.contains(.maskAlternate) {
            return Unmanaged.passUnretained(event)
        }
        
        var length = 0
        var chars = [UniChar](repeating: 0, count: 10)
        event.keyboardGetUnicodeString(maxStringLength: 10, actualStringLength: &length, unicodeString: &chars)
        
        guard length > 0 else {
            return Unmanaged.passUnretained(event)
        }
        
        let text = String(utf16CodeUnits: chars, count: length)
        
        guard text.count == 1, let char = text.first, char.isLetter else {
            return Unmanaged.passUnretained(event)
        }
        
        // Caesar cipher: shift by 7
        let isUpper = char.isUppercase
        let base = isUpper ? Character("A") : Character("a")
        let offset = Int(char.asciiValue!) - Int(base.asciiValue!)
        let shifted = (offset + 7) % 26
        let newChar = Character(UnicodeScalar(Int(base.asciiValue!) + shifted)!)
        
        var newChars = [UniChar](String(newChar).utf16)
        event.keyboardSetUnicodeString(stringLength: newChars.count, unicodeString: &newChars)
        
        return Unmanaged.passUnretained(event)
    }
    
    deinit {
        stop()
    }
}

struct ContentView_Previews: PreviewProvider {
    static var previews: some View {
        ContentView()
    }
}
