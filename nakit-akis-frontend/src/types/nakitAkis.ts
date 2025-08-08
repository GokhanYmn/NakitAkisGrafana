
export interface NakitAkisParametre{
    faizOrani:number;
    kaynakKurulus:string;
    secilenBankalar:string[];
    baslangicTarihi?:Date;
    bitisTarihi?:Date;
}

export interface NakitAkisSonuc{
    toplamFaizTutari:number;
    toplamModelFaizTutari:number;
    farkTutari:number;
    farkYuzdesi:number;
}

export interface GrafanaVariable{
    text:string;
    value:string;
}

export interface TrendData{
    timestamp:number;
    hafta:string;
    fon_no:string;
    kumulatif_mevduat:number;
    kumulatif_faiz_kazanci:number;
    kumulatif_buyume_yuzde:number;
    kurulus:string;
}