export const LoadingSpinner = () => {
  return (
    <>
      <style>{`
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
        .roima-spinner-minimal {
          width: 32px;
          height: 32px;
          border: 2px solid #d1fae5;
          border-top: 2px solid #10b981;
          border-radius: 50%;
          animation: spin 1s linear infinite;
        }
        .roima-container-minimal {
          display: flex;
          align-items: center;
          justify-content: center;
          position: relative;
        }
        .roima-r-minimal {
          position: absolute;
          font-size: 12px;
          font-weight: bold;
          color: #047857;
        }
      `}</style>
      <div className="roima-container-minimal">
        <div className="roima-spinner-minimal"></div>
        <span className="roima-r-minimal">R</span>
      </div>
    </>
  );
};
