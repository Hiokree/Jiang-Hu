import { DependencyContainer } from "tsyringe";

// SPT Types
import { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import { ILogger } from "@spt/models/spt/utils/ILogger";
import { PreSptModLoader } from "@spt/loaders/PreSptModLoader";
import { DatabaseServer } from "@spt/servers/DatabaseServer";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { ConfigServer } from "@spt/servers/ConfigServer";
import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import { Traders } from "@spt/models/enums/Traders";
import { ITraderConfig, UpdateTime } from "@spt/models/spt/config/ITraderConfig";
import { ITraderBase, ITraderAssort } from "@spt/models/eft/common/tables/ITrader";
import { IRagfairConfig } from "@spt/models/spt/config/IRagfairConfig";
import { ImageRouter } from "@spt/routers/ImageRouter";
import { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import { JsonUtil } from "@spt/utils/JsonUtil";

// Trader Settings
import * as baseJson from "../db/base.json";
import * as assortJson from "../db/assort.json";

class LoongGateInn implements IPreSptLoadMod, IPostDBLoadMod {
    private mod: string;
    private logger: ILogger;

    // Set name of mod so we can log it to console later
    constructor() {
        this.mod = "Jiang Hu"
    }

    /*
     * Some work needs to be done prior to SPT code being loaded, registering the profile image + setting trader update time inside the trader config json
     * @param container dependency container
     */
    public preSptLoad(container: DependencyContainer): void {
        // Get a logger
        this.logger = container.resolve<ILogger>("WinstonLogger");
        this.logger.debug(`[${this.mod}] preSpt loading...`);

        // Get SPT code/data we need later
        const preSptModLoader: PreSptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        const imageRouter: ImageRouter = container.resolve<ImageRouter>("ImageRouter");
        const hashUtil: HashUtil = container.resolve<HashUtil>("HashUtil");
        const configServer = container.resolve<ConfigServer>("ConfigServer");
        const traderConfig: ITraderConfig = configServer.getConfig<ITraderConfig>(ConfigTypes.TRADER);
        const ragfairConfig = configServer.getConfig<IRagfairConfig>(ConfigTypes.RAGFAIR);

        this.registerProfileImage(preSptModLoader, imageRouter);
        this.setTraderUpdateTime(traderConfig);

        // Add trader to trader enum
        Traders[baseJson._id] = baseJson._id;

        // Add trader to flea market	
        ragfairConfig.traders[baseJson._id] = true;

        this.logger.debug(`[${this.mod}] preSpt loaded`);
    }

    /*
     * Majority of trader-related work occurs after the aki database has been loaded but prior to SPT code being run
     * @param container dependency container
     */
    public postDBLoad(container: DependencyContainer): void {
        this.logger.debug(`[${this.mod}] postDB loading...`);

        const db: DatabaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        const configServer: ConfigServer = container.resolve<ConfigServer>("ConfigServer");
        const tables = db.getTables();
        const jsonUtil: JsonUtil = container.resolve<JsonUtil>("JsonUtil");

        this.addTraderToDB(baseJson, tables, jsonUtil);

        // Add trader to locale file, ensures trader text shows properly on screen
        // WARNING: adds the same text to ALL locales (e.g. chinese/english)
        this.addTraderToLocales(tables, baseJson.name, "", baseJson.nickname, baseJson.location, "");

        this.logger.debug(`[${this.mod}] loaded`);
    }

    private registerProfileImage(preSptModLoader: PreSptModLoader, imageRouter: ImageRouter): void {
        const imageFilePath = `./${preSptModLoader.getModPath(this.mod)}res`;
        imageRouter.addRoute(baseJson.avatar.replace(".jpg", ""), `${imageFilePath}/LoongGateInn.jpg`);
    }

    private setTraderUpdateTime(traderConfig: ITraderConfig): void {
        const traderRefreshRecord: UpdateTime = {
            traderId: baseJson._id,
            seconds: {
                min: 14400,
                max: 18000
            } };
        traderConfig.updateTime.push(traderRefreshRecord);
    }

    private addTraderToDB(LoongGateInn: any, tables: IDatabaseTables, jsonUtil: jsonUtil): void {
        tables.traders[LoongGateInn._id] = {
            assort: jsonUtil.deserialize(jsonUtil.serialize(assortJson)) as ITraderAssort,
            base: jsonUtil.deserialize(jsonUtil.serialize(LoongGateInn)) as ITraderBase,
            questassort: {
                started: {},
                success: {},
                fail: {}
            }
        };
    }

    private addTraderToLocales(tables: IDatabaseTables, fullName: string, firstName: string, nickName: string, location: string, description: string) {
        const locales = Object.values(tables.locales.global) as RecordM<string, string>[];
        for (const locale of locales) {
            locale[`${baseJson._id} Fullname`] = fullName;
            locale[`${baseJson._id} FirstName`] = firstName;
            locale[`${baseJson._id} Nickname`] = nickName;
            locale[`${baseJson._id} Location`] = location;
            locale[`${baseJson._id} Description`] = description;
        }
    }
}
module.exports = { mod: new LoongGateInn() }