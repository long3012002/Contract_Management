import useMFASetup from '../hooks/useMFASetup';
import logo from '../../../assets/logo/logo_Co-opBank.png';

// Import step components
import StepIntro from './setup-steps/StepIntro';
import StepDownloadApp from './setup-steps/StepDownloadApp';
import StepScanQr from './setup-steps/StepScanQr';
import StepVerifyOtp from './setup-steps/StepVerifyOtp';
import StepSuccess from './setup-steps/StepSuccess';

export default function MFASetupWizard() {
  const {
    tempUser,
    step,
    otp,
    error,
    isLoading,
    trustDevice,
    secretKey,
    setOtp,
    setTrustDevice,
    handleNextStep,
    handlePrevStep,
    handleCopyKey,
    handleVerify,
    handleFinish,
    cancelMfa,
  } = useMFASetup();

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
            {[1, 2, 3, 4].map((num) => (
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

        {/* Dynamic step component rendering */}
        {step === 1 && <StepIntro onNext={handleNextStep} onCancel={cancelMfa} />}
        {step === 2 && <StepDownloadApp onNext={handleNextStep} onBack={handlePrevStep} />}
        {step === 3 && (
          <StepScanQr
            secretKey={secretKey}
            onCopyKey={handleCopyKey}
            onNext={handleNextStep}
            onBack={handlePrevStep}
          />
        )}
        {step === 4 && (
          <StepVerifyOtp
            otp={otp}
            error={error}
            isLoading={isLoading}
            onInputChange={setOtp}
            onVerify={handleVerify}
            onBack={handlePrevStep}
          />
        )}
        {step === 5 && (
          <StepSuccess
            trustDevice={trustDevice}
            onTrustChange={setTrustDevice}
            onFinish={handleFinish}
          />
        )}
      </div>
    </div>
  );
}
