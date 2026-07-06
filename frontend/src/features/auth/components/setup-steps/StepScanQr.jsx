import { memo } from 'react';
import Button from '../../../../components/Button/Button';

const StepScanQr = memo(function StepScanQr({ secretKey, qrCodeUrl, onCopyKey, onNext, onBack }) {
  return (
    <div className="space-y-3.5 animate-fade-in">
      {/* QR Code Container - Compacted to 112px (w-28 h-28) */}
      <div className="flex justify-center p-1 bg-white border border-border rounded-custom w-28 h-28 mx-auto items-center shadow-inner relative group">
        {qrCodeUrl ? (
          <img
            src={`https://api.qrserver.com/v1/create-qr-code/?size=112x112&data=${encodeURIComponent(qrCodeUrl)}`}
            alt="Mã QR 2FA Co-opBank"
            className="w-24 h-24 object-contain"
          />
        ) : (
          <svg className="w-24 h-24" viewBox="0 0 100 100" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M5 5h30v30H5V5zm4 4v22h22V9H9zM5 65h30v30H5V65zm4 4v22h22V69H9zm56-64h30v30H65V5zm4 4v22h22V9H69z" fill="currentColor" className="text-slate-800" />
            <rect x="15" y="15" width="10" height="10" fill="currentColor" className="text-slate-800" />
            <rect x="15" y="75" width="10" height="10" fill="currentColor" className="text-slate-800" />
            <rect x="75" y="15" width="10" height="10" fill="currentColor" className="text-slate-800" />
            <path d="M45 5h10v10H45V5zm10 20h10v10H55V25zm-10 10h10v10H45V35zm30 10h10v15H75V45zm-20 15h10v10H55V60zm10 10h10v10H65V70zm10 10h15v10H75V80zm-30 0h10v15H45V80zm10 5h10v10H55V85zm25-30h10v10H80V55zM45 50h10v10H45V50z" fill="currentColor" className="text-slate-800" />
          </svg>
        )}
        <div className="absolute inset-0 bg-white/95 opacity-0 group-hover:opacity-100 flex items-center justify-center transition-all p-2 text-center rounded-custom">
          <p className="text-[10px] font-semibold text-slate-700">Mã QR 2FA Co-opBank</p>
        </div>
      </div>

      <div className="space-y-1.5 text-center">
        <div className="text-[13px] text-muted-foreground font-medium">
          Quét mã QR bằng ứng dụng Authenticator hoặc dán khóa:
        </div>
        <div className="flex items-center gap-1.5 bg-slate-50 border border-border px-3 py-1.5 rounded-custom justify-between">
          <span className="font-mono text-xs font-bold text-slate-700 tracking-wider">
            {secretKey}
          </span>
          <button
            type="button"
            onClick={onCopyKey}
            className="text-primary hover:text-primary-foreground hover:bg-primary px-2 py-0.5 rounded text-[11px] font-medium border border-primary/20 transition-all cursor-pointer"
          >
            Copy
          </button>
        </div>
      </div>

      {/* Instructions - Reduced to 2 simple points to save significant vertical space */}
      <div className="bg-slate-50 p-2.5 border border-border/80 rounded-custom space-y-1">
        <p className="text-xs font-bold text-primary flex items-center gap-1">
          <span>💡</span> Hướng dẫn nhanh:
        </p>
        <ul className="text-xs text-muted-foreground space-y-1 pl-4 list-decimal leading-normal">
          <li>Mở app <strong>Google Authenticator</strong>, nhấn dấu <strong>`+`</strong> ở dưới cùng bên phải.</li>
          <li>Chọn <strong>Quét mã QR</strong> (hoặc chọn <strong>Nhập khóa thiết lập</strong> và dán mã Secret Key ở trên).</li>
        </ul>
      </div>

      {/* Buttons container */}
      <div className="flex gap-3 pt-1">
        <button
          type="button"
          onClick={onBack}
          className="flex-1 py-2 px-4 bg-muted hover:bg-slate-200 text-muted-foreground font-semibold rounded-custom transition-all text-sm cursor-pointer"
        >
          Quay lại
        </button>
        <Button className="flex-1" onClick={onNext}>Tiếp tục</Button>
      </div>
    </div>
  );
});

export default StepScanQr;
