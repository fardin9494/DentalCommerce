export function receiptDisplayRef(id: string, reason: string | null | undefined, externalRef: string | null | undefined): string {
  void id
  void reason
  return externalRef?.trim() || '-'
}

export function issueDisplayRef(id: string, externalRef: string | null | undefined): string {
  void id
  return externalRef?.trim() || '-'
}

export function transferDisplayRef(id: string, externalRef: string | null | undefined): string {
  void id
  return externalRef?.trim() || '-'
}

export function adjustmentDisplayNote(id: string, reason: string | null | undefined, note: string | null | undefined): string {
  void id
  void reason
  return note?.trim() || '-'
}
