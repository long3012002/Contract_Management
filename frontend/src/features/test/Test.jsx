import { useMemo, useState } from "react";

const products = []

export default function Test() {
  const [keyword, setKeyword] = useState("");
  const [count, setCount] = useState(0);

  console.log("Filter running...");

  const filteredProducts = useMemo(() => {
    console.log("Filter running...");

    return products.filter((p) =>
      p.name.toLowerCase().includes(keyword.toLowerCase())
    );
  }, [keyword])

  return (
    <>
      <input
        value={keyword}
        onChange={(e) => setKeyword(e.target.value)}
      />

      <button onClick={() => setCount((c) => c + 1)}>
        Count: {count}
      </button>

      <p>{filteredProducts.length}</p>
    </>
  );
}