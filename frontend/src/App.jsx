import { Route, Routes } from "react-router-dom";
import Login from "./pages/Login";
import Test from "./features/test/Test";
import Permissions from "./pages/Permissions";
import Index from "./pages/Index";
import ContractManagement from "./pages/ContractManagement";

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route element={<CheckAuth />}>
        <Route path="/" element={<Index />} />
        <Route path="/test" element={<Test />} />
        <Route path="/permissions" element={<Permissions />} />
        <Route path="/contracts" element={<ContractManagement />} />
      </Route>
    </Routes>
  )
}

export default App