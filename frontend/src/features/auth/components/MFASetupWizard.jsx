import { MFASetupProvider, useMFASetupContext } from '../context/MFASetupContext';
import logo from '../../../assets/logo/logo_Co-opBank.png';

// Import step components
import StepIntro from './setup-steps/StepIntro';
import StepDownloadApp from './setup-steps/StepDownloadApp';
import StepScanQr from './setup-steps/StepScanQr';
import StepVerifyOtp from './setup-steps/StepVerifyOtp';
import StepSuccess from './setup-steps/StepSuccess';

// Hoist static data ra module level — tránh tạo lại array mỗi render (rendering-hoist-jsx)
const STEP_NUMBERS = [1, 2, 3, 4];

function MFASetupWizardContent() {
  const { tempUser, step } = useMFASetupContext();

  return (
    <div className="relative z-10 w-full max-w-[400px] bg-white/90 backdrop-blur-md rounded-custom border border-white/30 shadow-2xl overflow-hidden transition-all duration-300 hover:shadow-slate-950/30">
      <div className="p-6">
        {/* Logo & Header */}
        <div className="flex flex-col items-center mb-4">
          <img
            src={logo}
            alt="Co-opBank Logo"
            className="h-11 object-contain mb-2"
          />
          <h1 className="text-xl font-bold text-card-foreground text-center font-display tracking-tight uppercase">
            Thiết lập bảo mật 2 lớp
          </h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Tài khoản: <span className="font-semibold text-primary">{tempUser?.username}</span>
          </p>
        </div>

        {/* Stepper progress */}
        {step < 5 ? (
          <div className="flex justify-between items-center mb-4 px-4">
            {STEP_NUMBERS.map((num) => (
              <div key={num} className="flex items-center flex-1 last:flex-initial">
                <div
                  className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-semibold transition-all ${
                    step === num
                      ? 'bg-primary text-white scale-110 shadow-md ring-2 ring-primary/20'
                      : step > num
                      ? 'bg-emerald-500 text-white'
                      : 'bg-muted text-muted-foreground'
                  }`}
                >
                  {step > num ? '✓' : num}
                </div>
                {num < 4 ? (
                  <div
                    className={`h-0.5 flex-1 mx-2 transition-all ${
                      step > num ? 'bg-emerald-500' : 'bg-muted'
                    }`}
                  />
                ) : null}
              </div>
            ))}
          </div>
        ) : null}

        {/* Dynamic step component rendering — dùng ternary thay vị && để tránh render số nguyên 0 (rendering-conditional-render) */}
        {step === 1 ? <StepIntro /> : null}
        {step === 2 ? <StepDownloadApp /> : null}
        {step === 3 ? <StepScanQr /> : null}
        {step === 4 ? <StepVerifyOtp /> : null}
        {step === 5 ? <StepSuccess /> : null}
      </div>
    </div>
  );
}

export default function MFASetupWizard() {
  return (
    <MFASetupProvider>
      <MFASetupWizardContent />
    </MFASetupProvider>
  );
}
